using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Requra.Application.DTOs;
using Requra.Application.DTOs.AI;
using Requra.Application.DTOs.Project.Requirements;
using Requra.Application.Interfaces.IProjectService.IRequirementService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Requra.Infrastructure.Services.ProjectService.RequirementService
{
    public class RequirementService(RequraDbContext _context) : IRequirementService
    {
        public async Task<Response<UpdateRequirementStatusResponse>> UpdateRequirementStatusAsync(UpdateRequirementStatusRequest request, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request.ProjectId == Guid.Empty)
                errors.Add("ProjectId is required.");

            if (request.RequirementId == Guid.Empty)
                errors.Add("RequirementId is required.");

            if (request.WorkflowStatus != RequirementStatus.Approved &&
                request.WorkflowStatus != RequirementStatus.Rejected &&
                request.WorkflowStatus != RequirementStatus.NeedsReview)
            {
                errors.Add("WorkflowStatus must be Approved, Rejected, or NeedsReview.");
            }
            if ((request.WorkflowStatus == RequirementStatus.Rejected ||
                 request.WorkflowStatus == RequirementStatus.NeedsReview) &&
                 string.IsNullOrWhiteSpace(request.ReviewFeedback))
            {
                errors.Add("ReviewFeedback is required when rejecting or requesting review.");
            }

            if (!string.IsNullOrWhiteSpace(request.ReviewFeedback) && request.ReviewFeedback.Length > 4000)
                errors.Add("ReviewFeedback must not exceed 4000 characters.");

            if (errors.Any())
            {
                return Response<UpdateRequirementStatusResponse>.Failure(new UpdateRequirementStatusResponse(), "Validation failed.", StatusCodes.Status400BadRequest, errors);
            }

            if (string.IsNullOrWhiteSpace(request.ReviewedById))
            {
                return Response<UpdateRequirementStatusResponse>.Failure(new UpdateRequirementStatusResponse(), "Current user is not authenticated.", StatusCodes.Status401Unauthorized);
            }

            try
            {
                var requirement = await _context.Requirements
                    .FirstOrDefaultAsync(x => x.Id == request.RequirementId && x.ProjectId == request.ProjectId, cancellationToken);

                if (requirement is null)
                {
                    return Response<UpdateRequirementStatusResponse>.Failure(new UpdateRequirementStatusResponse(), "Requirement not found.", StatusCodes.Status404NotFound);
                }

                var reviewer = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.ReviewedById, cancellationToken);

                if (reviewer is null)
                {
                    return Response<UpdateRequirementStatusResponse>.Failure(new UpdateRequirementStatusResponse(), "Reviewer not found.", StatusCodes.Status404NotFound);
                }


                if (string.IsNullOrWhiteSpace(request.IfMatch))
                {
                    return Response<UpdateRequirementStatusResponse>.Failure(new UpdateRequirementStatusResponse(),"If-Match header is required.",StatusCodes.Status428PreconditionRequired);
                }

                if (!MatchesIfMatch(request.IfMatch, requirement.Version))
                {
                    return Response<UpdateRequirementStatusResponse>.Failure(new UpdateRequirementStatusResponse(),"The requirement has been modified by another user. Please refresh and try again.",StatusCodes.Status412PreconditionFailed);
                }


                switch (request.WorkflowStatus)
                {
                    case RequirementStatus.Approved:
                        requirement.Approve(request.ReviewedById, request.ReviewFeedback?.Trim());
                        break;

                    case RequirementStatus.Rejected:
                        requirement.Reject(request.ReviewedById, request.ReviewFeedback?.Trim());
                        break;

                    case RequirementStatus.NeedsReview:
                        requirement.FlagForReview(request.ReviewedById, request.ReviewFeedback?.Trim());
                        break;
                }

                await _context.SaveChangesAsync(cancellationToken);

                var response = new UpdateRequirementStatusResponse
                {
                    Id = requirement.SourceRequirementId,
                    ProjectId = requirement.ProjectId ?? Guid.Empty,
                    WorkflowStatus = requirement.Status.ToString().ToUpper(),
                    ReviewFeedback = requirement.ReviewFeedback,
                    ReviewedBy = requirement.ReviewedById,
                    ReviewedAt = requirement.ReviewedAt,
                    UpdatedAt = requirement.UpdatedAt,
                    Version = requirement.Version
                };

                return Response<UpdateRequirementStatusResponse>.Success(response, "Requirement status updated successfully.", StatusCodes.Status200OK);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<UpdateRequirementStatusResponse>.Failure(new UpdateRequirementStatusResponse(), "A concurrency error occurred while updating requirement status.", StatusCodes.Status409Conflict, new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<UpdateRequirementStatusResponse>.Failure(new UpdateRequirementStatusResponse(), "A database error occurred while updating requirement status.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                return Response<UpdateRequirementStatusResponse>.Failure(new UpdateRequirementStatusResponse(), "An unexpected error occurred while updating requirement status.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Response<EditRequirementContentResponse>> EditRequirementContentAsync(EditRequirementContentRequest request,CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request.ProjectId == Guid.Empty)
                errors.Add("ProjectId is required.");

            if (request.RequirementId == Guid.Empty)
                errors.Add("RequirementId is required.");

            if (request.Title != null && string.IsNullOrWhiteSpace(request.Title))
                errors.Add("Title cannot be empty.");

            if (request.Title != null && request.Title.Length > 300)
                errors.Add("Title must not exceed 300 characters.");

            if (request.Description != null && request.Description.Length > 4000)
                errors.Add("Description must not exceed 4000 characters.");

            if (request.Priority != null && request.Priority.Length > 50)
                errors.Add("Priority must not exceed 50 characters.");

            if (request.Title == null &&
                request.Description == null &&
                request.Type == null &&
                request.Priority == null)
            {
                errors.Add("At least one field must be provided for update.");
            }

            if (errors.Any())
            {
                return Response<EditRequirementContentResponse>.Failure(new EditRequirementContentResponse(),"Validation failed.",StatusCodes.Status400BadRequest,errors);
            }

            if (string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                return Response<EditRequirementContentResponse>.Failure(new EditRequirementContentResponse(),"Current user is not authenticated.",StatusCodes.Status401Unauthorized);
            }

            try
            {
                var requirement = await _context.Requirements
                    .Include(x => x.RequirementSourceReferences)
                    .FirstOrDefaultAsync(
                        x => x.Id == request.RequirementId && x.ProjectId == request.ProjectId,
                        cancellationToken);

                if (requirement is null)
                {
                    return Response<EditRequirementContentResponse>.Failure(new EditRequirementContentResponse(),"Requirement not found.",StatusCodes.Status404NotFound);
                }

                var currentUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.CurrentUserId, cancellationToken);

                if (currentUser is null)
                {
                    return Response<EditRequirementContentResponse>.Failure(
                        new EditRequirementContentResponse(),
                        "Current user not found.",
                        StatusCodes.Status404NotFound);
                }

                if (string.IsNullOrWhiteSpace(request.IfMatch))
                {
                    return Response<EditRequirementContentResponse>.Failure(new EditRequirementContentResponse(),"If-Match header is required.",StatusCodes.Status428PreconditionRequired);
                }

                if (!MatchesIfMatch(request.IfMatch, requirement.Version))
                {
                    return Response<EditRequirementContentResponse>.Failure(new EditRequirementContentResponse(),"The requirement has been modified by another user. Please refresh and try again.",StatusCodes.Status412PreconditionFailed);
                }

                requirement.EditContent(
                    title: request.Title,
                    description: request.Description,
                    type: request.Type,
                    priority: request.Priority,
                    modifiedById: request.CurrentUserId);

                await UpdateRequirementInLatestAnalysisResultRawJsonAsync(requirement, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);


                var response = new EditRequirementContentResponse
                {
                    Id = requirement.SourceRequirementId,
                    ProjectId = requirement.ProjectId ?? Guid.Empty,
                    Title = requirement.Title,
                    Description = requirement.Description,
                    Type = requirement.Type.ToString(),
                    Priority = requirement.Priority,
                    ConfidenceScore = requirement.ConfidenceScore,
                    SourceDocumentIds = requirement.RequirementSourceReferences
                        .Where(x => x.DocumentId.HasValue)
                        .Select(x => x.DocumentId!.Value)
                        .Distinct()
                        .ToList(),
                    SourceRefs = requirement.RequirementSourceReferences
                        .Select(x => new Requra.Application.DTOs.Project.Requirements.RequirementSourceRefDto
                        {
                            SourceDocumentId = x.SourceId,
                            Document_Name = x.DocumentName,
                            Page = x.Page,
                            ChunkId = x.ChunkId,
                            Quote = x.Quote
                        })
                        .ToList(),
                    //Quality = new RequirementQualityDto
                    //{
                    //    Score = requirement.QualityScore,
                    //    Issues = string.IsNullOrWhiteSpace(requirement.QualityIssues)
                    //        ? new List<string>()
                    //        : requirement.QualityIssues
                    //            .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    //            .Select(x => x.Trim())
                    //            .ToList(),
                    //    Warnings = string.IsNullOrWhiteSpace(requirement.QualityWarnings)
                    //        ? new List<string>()
                    //        : requirement.QualityWarnings
                    //            .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    //            .Select(x => x.Trim())
                    //            .ToList()
                    //},
                    Quality = new Requra.Application.DTOs.Project.Requirements.RequirementQualityDto
                    {
                        Score = requirement.QualityScore,

                        Issues = string.IsNullOrWhiteSpace(requirement.QualityIssues)
                            ? new List<string>()
                            : JsonSerializer.Deserialize<List<string>>(
                                requirement.QualityIssues) ?? new List<string>(),

                        Warnings = string.IsNullOrWhiteSpace(requirement.QualityWarnings)
                            ? new List<string>()
                            : JsonSerializer.Deserialize<List<string>>(
                                requirement.QualityWarnings) ?? new List<string>()
                    },
                    QualityStatus = requirement.QualityStatus,
                    WorkflowStatus = requirement.Status.ToString().ToUpper(),
                    ReviewFeedback = requirement.ReviewFeedback,
                    ReviewedBy = requirement.ReviewedById,
                    ReviewedAt = requirement.ReviewedAt,
                    CreatedAt = requirement.CreatedAt,
                    UpdatedAt = requirement.UpdatedAt,
                    LastModifiedBy = requirement.LastModifiedById,
                    Version = requirement.Version
                };

                return Response<EditRequirementContentResponse>.Success(response,"Requirement updated successfully",StatusCodes.Status200OK);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<EditRequirementContentResponse>.Failure(new EditRequirementContentResponse(),"A concurrency error occurred while updating the requirement.",StatusCodes.Status409Conflict,new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<EditRequirementContentResponse>.Failure(new EditRequirementContentResponse(),"A database error occurred while updating the requirement.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                return Response<EditRequirementContentResponse>.Failure(new EditRequirementContentResponse(),"An unexpected error occurred while updating the requirement.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
        }

        public async Task<Response<PagedResult<RequirementsDto>>> GetRequirementsByProjectIdAsync(GetProjectRequirementsRequest request)
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
                return Response<PagedResult<RequirementsDto>>.Failure(new PagedResult<RequirementsDto>(),"Validation failed.",400,errors);
            }

            try
            {
                var projectExists = await _context.Projects.AsNoTracking().AnyAsync(x => x.Id == request.ProjectId);

                if (!projectExists)
                {
                    return Response<PagedResult<RequirementsDto>>.Failure(new PagedResult<RequirementsDto>(),"Project not found",404);
                }

                var query = _context.Requirements
                    .AsNoTracking()
                    .Include(x => x.RequirementSourceReferences)
                    .Include(x => x.UserStories)
                    .Where(x => x.ProjectId == request.ProjectId);

                if (request.Status != null && request.Status.Any())
                {
                    var normalizedStatuses = request.Status
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(NormalizeRequirementStatusFilter)
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
                        (!string.IsNullOrWhiteSpace(x.SourceRequirementId) && x.SourceRequirementId.ToLower().Contains(search)) ||
                        (!string.IsNullOrWhiteSpace(x.Title) && x.Title.ToLower().Contains(search)) ||
                        (!string.IsNullOrWhiteSpace(x.Description) && x.Description.ToLower().Contains(search)));
                }

                var totalCount = await query.CountAsync();

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

                var modifiedByIds = entities
                    .Where(x => !string.IsNullOrWhiteSpace(x.LastModifiedById))
                    .Select(x => x.LastModifiedById!)
                    .Distinct()
                    .ToList();

                var actorIds = reviewedByIds
                    .Union(modifiedByIds)
                    .Distinct()
                    .ToList();

                var users = await _context.Users
                    .AsNoTracking()
                    .Where(x => actorIds.Contains(x.Id))
                    .Select(x => new
                    {
                        x.Id,
                        Name = !string.IsNullOrWhiteSpace(x.FullName) ? x.FullName : x.UserName
                    })
                    .ToListAsync();

                var userMap = users.ToDictionary(x => x.Id, x => x.Name);

                var items = entities.Select(requirement =>
                {
                    var linkedStories = requirement.UserStories ?? new List<UserStory>();

                    var linkedUserStoryCount = linkedStories.Count;
                    var approvedUserStoryCount = linkedStories.Count(us => us.Status == UserStoryStatus.Approved);

                    var storyCoveragePercent = linkedUserStoryCount == 0
                        ? 0
                        : 100;

                    return new RequirementsDto
                    {
                        Id = requirement.Id,
                        SourceRequirementId = requirement.SourceRequirementId,
                        Title = requirement.Title,
                        Description = requirement.Description,
                        Type = requirement.Type.ToString().Replace("_", "-"),
                        Priority = requirement.Priority,
                        Actor = requirement.Actor,
                        Category = requirement.Category,

                        Status = NormalizeRequirementStatus(requirement.Status),
                        ReviewFeedback = requirement.ReviewFeedback,
                        ReviewedBy = !string.IsNullOrWhiteSpace(requirement.ReviewedById) && userMap.ContainsKey(requirement.ReviewedById)
                            ? userMap[requirement.ReviewedById]
                            : null,
                        ReviewedAt = requirement.ReviewedAt,
                        LastModifiedBy = !string.IsNullOrWhiteSpace(requirement.LastModifiedById) && userMap.ContainsKey(requirement.LastModifiedById)
                            ? userMap[requirement.LastModifiedById]
                            : null,
                        Version = requirement.Version,
                        UpdatedAt = requirement.UpdatedAt,

                        ConfidenceScore = requirement.ConfidenceScore,
                        Quality = new Requra.Application.DTOs.Project.Requirements.QualityDto
                        {
                            Score = requirement.QualityScore,
                            Issues = string.IsNullOrWhiteSpace(requirement.QualityIssues)
                                ? new List<string>()
                                : DeserializeStringList(requirement.QualityIssues),
                            Warnings = string.IsNullOrWhiteSpace(requirement.QualityWarnings)
                                ? new List<string>()
                                : DeserializeStringList(requirement.QualityWarnings)
                        },
                        QualityStatus = requirement.QualityStatus,

                        SourceRefs = requirement.RequirementSourceReferences?
                            .Select(sr => new RequirementSourceDto
                            {
                                SourceId = sr.SourceId,
                                SourceType = sr.SourceType,
                                DocumentName = sr.DocumentName,
                                Page = sr.Page,
                                ChunkId = sr.ChunkId,
                                Quote = sr.Quote,
                                ConfidenceScore = sr.ConfidenceScore
                            })
                            .ToList() ?? new List<RequirementSourceDto>(),

                        LinkedUserStoryCount = linkedUserStoryCount,
                        ApprovedUserStoryCount = approvedUserStoryCount,
                        StoryCoveragePercent = storyCoveragePercent,

                        LinkedUserStories = linkedStories
                            .OrderByDescending(us => us.CreatedAt)
                            .Select(us => new RequirementLinkedUserStoryDto
                            {
                                Id = us.Id,
                                SourceUserStoryId = us.SourceUserStoryId,
                                Title = us.Title,
                                Status = NormalizeUserStoryStatus(us.Status)
                            })
                            .ToList()
                    };
                }).ToList();

                var result = new PagedResult<RequirementsDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalCount == 0
                        ? 0
                        : (int)Math.Ceiling(totalCount / (double)request.PageSize)
                };


                return items.Any()
                    ? Response<PagedResult<RequirementsDto>>.Success(result,"Requirements fetched successfully",200)
                    : Response<PagedResult<RequirementsDto>>.Success(new PagedResult<RequirementsDto>(),"No requirements found",204);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error retrieving requirements for project {ProjectId}", projectId);

                return Response<PagedResult<RequirementsDto>>.Failure(new PagedResult<RequirementsDto>(),"An unexpected error occurred while retrieving requirements",500,new List<string> { ex.Message });
            }
        }



        private static string BuildRequirementETag(int? version)
        {
            return $"\"{version}\"";
        }

        private static bool MatchesIfMatch(string? ifMatch, int? currentVersion)
        {
            if (string.IsNullOrWhiteSpace(ifMatch))
                return false;

            var expected = BuildRequirementETag(currentVersion);
            return string.Equals(ifMatch.Trim(), expected, StringComparison.Ordinal);
        }



        private async Task<AnalysisResult?> GetLatestAnalysisResultForProjectAsync(Guid projectId, CancellationToken cancellationToken)
        {
            var analysisRun = await _context.AnalysisRuns
                .AsNoTracking()
                .Where(x =>
                    x.ProjectId == projectId &&
                    (x.Status == AnalysisRunStatus.COMPLETED || x.Status == AnalysisRunStatus.PARTIAL))
                .OrderByDescending(x => x.CompletedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (analysisRun == null)
                return null;

            return await _context.AnalysisResults
                .FirstOrDefaultAsync(x => x.AnalysisRunId == analysisRun.Id, cancellationToken);
        }

        private async Task UpdateRequirementInLatestAnalysisResultRawJsonAsync(Requirement requirement,CancellationToken cancellationToken)
        {
            if (!requirement.ProjectId.HasValue)
                return;

            var analysisResult = await GetLatestAnalysisResultForProjectAsync(requirement.ProjectId.Value, cancellationToken);

            if (analysisResult == null || string.IsNullOrWhiteSpace(analysisResult.RawJson))
                return;

            ResultDto? rawDto;
            try
            {
                rawDto = JsonSerializer.Deserialize<ResultDto>(
                    analysisResult.RawJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch
            {
                return;
            }

            if (rawDto?.Requirements == null || rawDto.Requirements.Count == 0)
                return;

            var rawRequirement = rawDto.Requirements
                .FirstOrDefault(x => x.Id == requirement.SourceRequirementId);

            if (rawRequirement == null)
                return;

            rawRequirement.Title = requirement.Title;
            rawRequirement.Description = requirement.Description;
            rawRequirement.Type = requirement.Type.ToString();
            rawRequirement.Priority = requirement.Priority;
            


            analysisResult.RawJson = JsonSerializer.Serialize(rawDto, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            //await _context.SaveChangesAsync(cancellationToken);
        }

        private static List<string> DeserializeStringList(string value)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>();
            }
            catch
            {
                return value
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();
            }
        }


        private static string NormalizeRequirementStatus(RequirementStatus status)
        {
            return status switch
            {
                RequirementStatus.Generated => "GENERATED",
                RequirementStatus.NeedsReview => "NEEDS_REVIEW",
                RequirementStatus.Edited => "EDITED",
                RequirementStatus.Approved => "APPROVED",
                RequirementStatus.Rejected => "REJECTED",
                _ => status.ToString().ToUpperInvariant()
            };
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
        
        private static RequirementStatus? NormalizeRequirementStatusFilter(string? value)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return null;
        
                    return value.Trim().ToUpperInvariant() switch
                    {
                        "GENERATED" => RequirementStatus.Generated,
                        "NEEDS_REVIEW" => RequirementStatus.NeedsReview,
                        "EDITED" => RequirementStatus.Edited,
                        "APPROVED" => RequirementStatus.Approved,
                        "REJECTED" => RequirementStatus.Rejected,
                        _ => null
                    };
                }
        
        
        
        
    }
}
