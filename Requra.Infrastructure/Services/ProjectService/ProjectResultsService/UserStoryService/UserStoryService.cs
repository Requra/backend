using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Project.ProjectResults.UserStory;
using Requra.Application.DTOs.UserStories;
using Requra.Application.Interfaces.IProjectService.IProjectResultsService.IUserStoryService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Services.ProjectService.ProjectResultsService.UserStoryService
{
    public class UserStoryService(IUnitOfWork _unitOfWork, RequraDbContext _context, IMapper _mapper, ILogger<UserStoryService> _logger) : IUserStoryService
    {

        public async Task<Response<PagedResult<UserStoryListItemDto>>> GetUserStoriesByProjectIdAsync(GetProjectUserStoriesRequest request)
        {
            var errors = new List<string>();

            if (request.ProjectId == Guid.Empty)
                errors.Add("Invalid ProjectId.");

            if (request.PageNumber < 1)
                errors.Add("PageNumber must be greater than or equal to 1.");

            if (request.PageSize < 1 || request.PageSize > 100)
                errors.Add("PageSize must be between 1 and 100.");

            if (errors.Any())
            {
                return Response<PagedResult<UserStoryListItemDto>>.Failure(
                    new PagedResult<UserStoryListItemDto>(),
                    "Validation failed.",
                    400,
                    errors);
            }

            try
            {
                var projectExists = await _context.Projects
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == request.ProjectId);

                if (!projectExists)
                {
                    return Response<PagedResult<UserStoryListItemDto>>.Failure(
                        new PagedResult<UserStoryListItemDto>(),
                        "Project not found.",
                        404);
                }

                var query = _context.UserStories
                    .AsNoTracking()
                    .Include(x => x.Requirement)
                    .Include(x => x.SourceRefs)
                    .Include(x => x.Quality)
                    .Include(x => x.JiraFields)
                    .Where(x => x.ProjectId == request.ProjectId);

                if (request.Status != null && request.Status.Any())
                {
                    var normalizedStatuses = request.Status
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(NormalizeUserStoryStatusFilter)
                        .Where(x => x.HasValue)
                        .Select(x => x!.Value)
                        .Distinct()
                        .ToList();

                    if (normalizedStatuses.Any())
                    {
                        query = query.Where(x => normalizedStatuses.Contains(x.Status));
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var search = request.Search.Trim().ToLower();

                    query = query.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.SourceUserStoryId) && x.SourceUserStoryId.ToLower().Contains(search)) ||
                        (!string.IsNullOrWhiteSpace(x.Title) && x.Title.ToLower().Contains(search)) ||
                        (!string.IsNullOrWhiteSpace(x.Description) && x.Description.ToLower().Contains(search)) ||
                        (!string.IsNullOrWhiteSpace(x.SourceRequirementId) && x.SourceRequirementId.ToLower().Contains(search)));
                }

                var totalCount = await query.CountAsync();

                // Materialize because AcceptanceCriteria / Labels may use value converters
                var entities = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var reviewedByIds = entities
                    .Where(x => !string.IsNullOrWhiteSpace(x.ReviewedById))
                    .Select(x => x.ReviewedById!)
                    .Distinct()
                    .ToList();

                var users = await _context.Users
                    .AsNoTracking()
                    .Where(x => reviewedByIds.Contains(x.Id))
                    .Select(x => new
                    {
                        x.Id,
                        Name = !string.IsNullOrWhiteSpace(x.FullName) ? x.FullName : x.UserName
                    })
                    .ToListAsync();

                var userMap = users.ToDictionary(x => x.Id, x => x.Name);

                var items = entities.Select(userStory =>
                {
                    var labels = userStory.Labels ?? new List<string>();

                    var jiraLabels = userStory.JiraFields?.Labels ?? labels;

                    var jiraStoryPoints = userStory.JiraFields?.StoryPoints ?? userStory.StoryPoints;

                    var acceptanceCriteria = userStory.AcceptanceCriteria ?? new List<AcceptanceCriterion>();

                    return new UserStoryListItemDto
                    {
                        Id = userStory.Id,
                        SourceUserStoryId = userStory.SourceUserStoryId,

                        RequirementId = userStory.RequirementId,
                        SourceRequirementId = userStory.SourceRequirementId,
                        RequirementTitle = userStory.Requirement?.Title,

                        Title = userStory.Title,
                        UserStory = userStory.Description,
                        Description = userStory.Description,

                        Type = NormalizeUserStoryType(userStory.Type),
                        Priority = NormalizeUserStoryPriority(userStory.Priority),
                        Labels = jiraLabels?.ToList() ?? new List<string>(),
                        StoryPoints = jiraStoryPoints,

                        Jira = new UserStoryJiraDto
                        {
                            IssueType = userStory.JiraFields?.IssueType ?? "Story",
                            StoryPoints = jiraStoryPoints,
                            Labels = jiraLabels?.ToList() ?? new List<string>()
                        },

                        AcceptanceCriteria = acceptanceCriteria
                            .Select((ac, index) => new UserStoryAcceptanceCriterionDto
                            {
                                Id = BuildAcceptanceCriterionId(userStory.SourceUserStoryId, index + 1),
                                Text = ac.Text ?? string.Empty,
                                Format = ac.CriterionType ?? "given_when_then"
                            })
                            .ToList(),

                        Status = NormalizeUserStoryStatus(userStory.Status),
                        ReviewFeedback = userStory.ReviewFeedback,
                        ReviewedBy = !string.IsNullOrWhiteSpace(userStory.ReviewedById) && userMap.ContainsKey(userStory.ReviewedById)
                            ? userMap[userStory.ReviewedById]
                            : null,
                        ReviewedAt = userStory.ReviewedAt,
                        Version = userStory.Version,
                        UpdatedAt = userStory.UpdatedAt,

                        Quality = new UserStoryQualityDto
                        {
                            Score = userStory.Quality?.Score,
                            Issues = userStory.Quality?.Issues?.ToList() ?? new List<string>(),
                            Warnings = userStory.Quality?.Warnings?.ToList() ?? new List<string>()
                        },
                        QualityStatus = userStory.Quality?.QualityStatus.ToString(),

                        SourceRefs = userStory.SourceRefs?
                            .Select(sr => new UserStorySourceRefDto
                            {
                                SourceId = sr.SourceId,
                                SourceType = sr.SourceType,
                                DocumentName = sr.DocumentName,
                                Page = sr.Page?? 0 ,
                                ChunkId = sr.ChunkId,
                                Quote = sr.Quote,
                                ConfidenceScore = sr.ConfidenceScore
                            })
                            .ToList() ?? new List<UserStorySourceRefDto>()
                    };
                }).ToList();

                var result = new PagedResult<UserStoryListItemDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalCount == 0
                        ? 0
                        : (int)Math.Ceiling(totalCount / (double)request.PageSize)
                };

                return Response<PagedResult<UserStoryListItemDto>>.Success(
                    result,
                    "User stories fetched successfully.",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user stories for project {ProjectId}", request.ProjectId);

                return Response<PagedResult<UserStoryListItemDto>>.Failure(
                    new PagedResult<UserStoryListItemDto>(),
                    "An unexpected error occurred while retrieving user stories.",
                    500,
                    new List<string> { ex.Message });
            }
        }
        //public async Task<Response<PagedResult<UserStoryDto>>> GetUserStoriesByProjectIdAsync(Guid projectId)
        //{
        //    if (projectId == Guid.Empty)
        //        return Response<PagedResult<UserStoryDto>>.Failure(new PagedResult<UserStoryDto>(),"Invalid ProjectId",400);
        //    try
        //    {
        //        var projectRepo = _unitOfWork.Repository<Project>();
        //        var projectExists = await projectRepo.GetByIdAsync(projectId);

        //        if (projectExists == null)
        //            return Response<PagedResult<UserStoryDto>>.Failure(new PagedResult<UserStoryDto>(),"Project not found",404);

        //        //var query = _context.UserStories.AsNoTracking().Where(us => us.ProjectId == projectId);
        //        //var totalCount = await query.CountAsync();
        //        //var items = await query.OrderByDescending(us => us.CreatedAt).ProjectTo<UserStoryDto>(_mapper.ConfigurationProvider).ToListAsync();
        //        var query = _context.UserStories.AsNoTracking()
        //           .Include(us => us.Creator)
        //           .Where(us => us.ProjectId == projectId);
        //        var totalCount = await query.CountAsync();

        //        // AcceptanceCriteria is a jsonb column with a value converter (not a real
        //        // EF relation), so it can't be projected via ProjectTo/SQL translation.
        //        // Materialize the entities first, then map in-memory.
        //        var entities = await query.OrderByDescending(us => us.CreatedAt).ToListAsync();
        //        var items = _mapper.Map<List<UserStoryDto>>(entities);
        //        var result = new PagedResult<UserStoryDto>
        //        {
        //            TotalCount = totalCount,
        //            Items = items,
        //            PageNumber=1,
        //            PageSize=totalCount

        //        };

        //        return items.Any()
        //            ? Response<PagedResult<UserStoryDto>>.Success(result, "User stories fetched successfully", 200)
        //            : Response<PagedResult<UserStoryDto>>.Success(new PagedResult<UserStoryDto>(), "No user stories found", 204);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving user stories for project {ProjectId}", projectId);

        //        return Response<PagedResult<UserStoryDto>>.Failure(new PagedResult<UserStoryDto>(), "An unexpected error occurred while retrieving user stories",500,new List<string> { ex.Message });
        //    }
        //}
        // updates 

        public async Task<Response<UpdateUserStoryStatusResponse>> UpdateUserStoryStatusAsync(UpdateUserStoryStatusRequest request, CancellationToken cancellationToken = default)
        {
            if (request.ProjectId == Guid.Empty || request.StoryId == Guid.Empty)
                return Response<UpdateUserStoryStatusResponse>.Failure("ProjectId and StoryId are required.", 400);

            if (!TryParseWorkflowStatus(request.WorkflowStatus, out var workflowStatus))
                return Response<UpdateUserStoryStatusResponse>.Failure("workflowStatus must be one of APPROVED, REJECTED, NEEDS_REVIEW.", 400);

            if ((workflowStatus == UserStoryStatus.Rejected || workflowStatus == UserStoryStatus.NeedReview) &&
                string.IsNullOrWhiteSpace(request.Feedback))
                return Response<UpdateUserStoryStatusResponse>.Failure("feedback is required when rejecting or requesting review.", 400);

            if (!string.IsNullOrWhiteSpace(request.Feedback) && request.Feedback.Length > 4000)
                return Response<UpdateUserStoryStatusResponse>.Failure("Feedback must not exceed 4000 characters.", 400);

            if (string.IsNullOrWhiteSpace(request.ReviewedById))
                return Response<UpdateUserStoryStatusResponse>.Failure("Current user is not authenticated.", 401);

            try
            {
                var project = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

                if (project is null)
                    return Response<UpdateUserStoryStatusResponse>.Failure("Project not found.", 404);

                var isMember = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.ReviewedById, cancellationToken);

                if (!isMember)
                    return Response<UpdateUserStoryStatusResponse>.Failure("You are not allowed to review user stories on this project.", 403);

                var story = await _context.UserStories
                    .FirstOrDefaultAsync(x => x.Id == request.StoryId && x.ProjectId == request.ProjectId, cancellationToken);

                if (story is null)
                    return Response<UpdateUserStoryStatusResponse>.Failure("User story not found.", 404);

                if (story.Status == workflowStatus)
                    return Response<UpdateUserStoryStatusResponse>.Failure("The user story is already in the requested status.", 422);

                if (string.IsNullOrWhiteSpace(request.IfMatch) || !MatchesIfMatch(request.IfMatch, story.Version))
                    return Response<UpdateUserStoryStatusResponse>.Failure("The user story has been modified by another user. Please refresh and try again.", 409);

                switch (workflowStatus)
                {
                    case UserStoryStatus.Approved:
                        story.Approve(request.ReviewedById, request.Feedback?.Trim());
                        break;
                    case UserStoryStatus.Rejected:
                        story.Reject(request.ReviewedById, request.Feedback?.Trim());
                        break;
                    case UserStoryStatus.NeedReview:
                        story.FlagForReview(request.ReviewedById, request.Feedback?.Trim());
                        break;
                }

                await _context.SaveChangesAsync(cancellationToken);

                var response = new UpdateUserStoryStatusResponse
                {
                    Id = story.Id,
                    ProjectId = story.ProjectId,
                    WorkflowStatus = ToWorkflowStatusString(story.Status),
                    ReviewFeedback = story.ReviewFeedback,
                    ReviewedBy = story.ReviewedById,
                    ReviewedAt = story.ReviewedAt,
                    UpdatedAt = story.UpdatedAt,
                    Version = story.Version
                };

                return Response<UpdateUserStoryStatusResponse>.Success(response, "User story status updated successfully", 200);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<UpdateUserStoryStatusResponse>.Failure("A concurrency error occurred while updating the user story status.", 409, new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for user story {StoryId} in project {ProjectId}", request.StoryId, request.ProjectId);
                return Response<UpdateUserStoryStatusResponse>.Failure("An unexpected error occurred while updating the user story status.", 500, new List<string> { ex.Message });
            }
        }

        private static bool TryParseWorkflowStatus(string? value, out UserStoryStatus status)
        {
            switch (value?.Trim().ToUpperInvariant())
            {
                case "APPROVED":
                    status = UserStoryStatus.Approved;
                    return true;
                case "REJECTED":
                    status = UserStoryStatus.Rejected;
                    return true;
                case "NEEDS_REVIEW":
                    status = UserStoryStatus.NeedReview;
                    return true;
                default:
                    status = default;
                    return false;
            }
        }

        private static string ToWorkflowStatusString(UserStoryStatus status) =>
        status == UserStoryStatus.NeedReview ? "NEEDS_REVIEW" : status.ToString().ToUpper();

        //private static bool MatchesIfMatch(string ifMatch, int currentVersion) =>
        //string.Equals(ifMatch.Trim(), $"\"{currentVersion}\"", StringComparison.Ordinal);
        private static bool MatchesIfMatch(string ifMatch, int currentVersion)
        {
            // Accept both quoted ETag form ("2") and a bare version number (2),
            // and tolerate the weak-validator prefix (W/"2") per RFC 7232.
            var value = ifMatch.Trim();

            if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
                value = value[2..].Trim();

            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                value = value[1..^1];

            return string.Equals(value, currentVersion.ToString(), StringComparison.Ordinal);
        }
        public async Task<Response<EditUserStoryContentResponse>> EditUserStoryContentAsync(EditUserStoryContentRequest request, CancellationToken cancellationToken = default)
        {
            if (request.ProjectId == Guid.Empty || request.StoryId == Guid.Empty)
                return Response<EditUserStoryContentResponse>.Failure("ProjectId and StoryId are required.", 400);

            if (string.IsNullOrWhiteSpace(request.ModifiedById))
                return Response<EditUserStoryContentResponse>.Failure("Current user is not authenticated.", 401);

            UserStoryPriority? priority = null;
            if (!string.IsNullOrWhiteSpace(request.Priority))
            {
                if (!Enum.TryParse<UserStoryPriority>(request.Priority, ignoreCase: true, out var parsedPriority))
                    return Response<EditUserStoryContentResponse>.Failure("priority must be one of Low, Medium, High, Critical.", 400);
                priority = parsedPriority;
            }

            if (request.Title != null && request.Title.Length > 255)
                return Response<EditUserStoryContentResponse>.Failure("title must not exceed 255 characters.", 400);

            List<AcceptanceCriterion>? acceptanceCriteria = null;
            if (request.AcceptanceCriteria != null)
            {
                if (request.AcceptanceCriteria.Any(ac => string.IsNullOrWhiteSpace(ac.Text)))
                    return Response<EditUserStoryContentResponse>.Failure("Each acceptance criterion must include text.", 400);

                acceptanceCriteria = request.AcceptanceCriteria
                    .Select(ac => new AcceptanceCriterion(ac.Text!.Trim(), ac.Format, ac.Id))
                    .ToList();
            }

            try
            {
                var project = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

                if (project is null)
                    return Response<EditUserStoryContentResponse>.Failure("Project not found.", 404);

                var isMember = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.ModifiedById, cancellationToken);

                if (!isMember)
                    return Response<EditUserStoryContentResponse>.Failure("You are not allowed to edit user stories on this project.", 403);

                var story = await _context.UserStories
                    .Include(us => us.SourceRefs)
                    .Include(us => us.Quality)
                    .FirstOrDefaultAsync(x => x.Id == request.StoryId && x.ProjectId == request.ProjectId, cancellationToken);

                if (story is null)
                    return Response<EditUserStoryContentResponse>.Failure("User story not found.", 404);

                if (string.IsNullOrWhiteSpace(request.IfMatch) || !MatchesIfMatch(request.IfMatch, story.Version))
                    return Response<EditUserStoryContentResponse>.Failure("The user story has been modified by another user. Please refresh and try again.", 409);

                story.EditContent(request.Title, request.Description, acceptanceCriteria, priority, request.Labels, request.ModifiedById);

                await _context.SaveChangesAsync(cancellationToken);

                var response = new EditUserStoryContentResponse
                {
                    Id = story.Id,
                    Title = story.Title,
                    Description = story.Description,
                    UserStoryText = story.Description,
                    AcceptanceCriteria = story.AcceptanceCriteria.Select(ac => new AcceptanceCriterionDto
                    {
                        Id = ac.Id,
                        Text = ac.Text,
                        Format = ac.CriterionType
                    }).ToList(),
                    Priority = story.Priority.ToString(),
                    Labels = story.Labels,
                    RequirementId = story.RequirementId,
                    SourceRefs = story.SourceRefs.Select(sr => new SourceRefDto
                    {
                        Page = sr.Page,
                        Quote = sr.Quote,
                        ChunkId = sr.ChunkId,
                        SourceId = sr.SourceId,
                        SourceType = sr.SourceType,
                        DocumentName = sr.DocumentName,
                        ConfidenceScore = sr.ConfidenceScore
                    }).ToList(),
                    Quality = story.Quality == null ? null : new QualityDto
                    {
                        Score = story.Quality.Score,
                        Issues = story.Quality.Issues,
                        Warnings = story.Quality.Warnings,
                        QualityStatus = story.Quality.QualityStatus.ToString()
                    },
                    WorkflowStatus = ToWorkflowStatusString(story.Status),
                    ReviewFeedback = story.ReviewFeedback,
                    ReviewedBy = story.ReviewedById,
                    ReviewedAt = story.ReviewedAt,
                    CreatedAt = story.CreatedAt,
                    UpdatedAt = story.UpdatedAt,
                    LastModifiedBy = story.LastModifiedBy,
                    Version = story.Version,
                    RevisionNumber = story.RevisionNumber,
                    RevisionSource = story.RevisionSource.ToString()
                };

                return Response<EditUserStoryContentResponse>.Success(response, "User story updated successfully", 200);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<EditUserStoryContentResponse>.Failure("A concurrency error occurred while updating the user story.", 409, new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing content for user story {StoryId} in project {ProjectId}", request.StoryId, request.ProjectId);
                return Response<EditUserStoryContentResponse>.Failure("An unexpected error occurred while updating the user story.", 500, new List<string> { ex.Message });
            }
        }



        private static string NormalizeUserStoryStatus(UserStoryStatus status)
        {
            return status switch
            {
                UserStoryStatus.Generated => "GENERATED",
                UserStoryStatus.NeedReview => "NEEDS_REVIEW",
                UserStoryStatus.Edited => "EDITED",
                UserStoryStatus.Approved => "APPROVED",
                UserStoryStatus.Rejected => "REJECTED",
                _ => status.ToString().ToUpperInvariant()
            };
        }
        private static UserStoryStatus? NormalizeUserStoryStatusFilter(string? value)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return null;
        
                    return value.Trim().ToUpperInvariant() switch
                    {
                        "GENERATED" => UserStoryStatus.Generated,
                        "NEEDS_REVIEW" => UserStoryStatus.NeedReview,
                        "EDITED" => UserStoryStatus.Edited,
                        "APPROVED" => UserStoryStatus.Approved,
                        "REJECTED" => UserStoryStatus.Rejected,
                        _ => null
                    };
                }
        private static string NormalizeUserStoryType(UserStoryType type)
        {
            return type switch
            {
                UserStoryType.Functional => "Functional",
                UserStoryType.NonFunctional => "Non-Functional",
                UserStoryType.BusinessRule => "Business",
                _ => type.ToString()
            };
        }
        
        private static string NormalizeUserStoryPriority(UserStoryPriority priority)
        {
            return priority switch
            {
                UserStoryPriority.low => "Low",
                UserStoryPriority.medium => "Medium",
                UserStoryPriority.high => "High",
                UserStoryPriority.critical => "Critical",
                _ => priority.ToString()
            };
        }
        private static string BuildAcceptanceCriterionId(string? sourceUserStoryId, int index)
                {
                    var storyId = string.IsNullOrWhiteSpace(sourceUserStoryId) ? "US-UNK" : sourceUserStoryId.Trim();
                    return $"AC-{storyId.Replace("US-", string.Empty)}-{index:D2}";
                }
        
            }
        }
