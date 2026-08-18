using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Requra.Application.DTOs.AI;
using Requra.Application.DTOs.Project.ProjectResults.Feedbacks;
using Requra.Application.DTOs.ProjectReviewInvitaion;
using Requra.Application.Interfaces.IProjectService.IProjectReviewService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.IEmailSender;
using Requra.Infrastructure.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Requra.Infrastructure.Services.ProjectService.ProjectReviewService
{
    public class ProjectReviewService(RequraDbContext _context, IEmailSender emailSender,ILogger<ProjectReviewService> logger, IValidator<CreateProjectReviewInvitationRequest> validator, IOptions<ProjectReviewLinkOptions> _projectReviewLinkOptions) : IProjectReviewService
    {
        public async Task<Response<SubmitStakeholderFeedbackResponse>> SubmitStakeholderFeedbackAsync(SubmitStakeholderFeedbackRequest request, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (!Enum.IsDefined(typeof(FeedbackTargetType), request.TargetType))
                errors.Add("TargetType is invalid.");

            if (request.TargetId == Guid.Empty)
                errors.Add("TargetId is required.");

            if (string.IsNullOrWhiteSpace(request.Content))
                errors.Add("Content is required.");
            else if (request.Content.Length > 4000)
                errors.Add("Content must not exceed 4000 characters.");

            if (errors.Any())
            {
                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "Validation failed.", StatusCodes.Status400BadRequest, errors);
            }

            if (string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "Current user is not authenticated.", StatusCodes.Status401Unauthorized);
            }

            try
            {
                var author = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.CurrentUserId, cancellationToken);

                if (author is null)
                {
                    return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "Author not found.", StatusCodes.Status404NotFound);
                }

                Guid projectId;
                string? targetTitle;

                switch (request.TargetType)
                {
                    case FeedbackTargetType.USER_STORY:
                        {
                            var userStory = await _context.UserStories.FirstOrDefaultAsync(x => x.Id == request.TargetId, cancellationToken);

                            if (userStory is null)
                            {
                                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "User story not found.", StatusCodes.Status404NotFound);
                            }

                            projectId = userStory.ProjectId;
                            targetTitle = userStory.Title;
                            break;
                        }

                    case FeedbackTargetType.REQUIREMENT:
                        {
                            var requirement = await _context.Requirements.FirstOrDefaultAsync(x => x.Id == request.TargetId, cancellationToken);

                            if (requirement is null)
                            {
                                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "Requirement not found.", StatusCodes.Status404NotFound);
                            }
                            if (!requirement.ProjectId.HasValue)
                            {
                                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(),"Requirement is not associated with a project.",StatusCodes.Status400BadRequest);
                            }

                            projectId = requirement.ProjectId.Value;
                            targetTitle = requirement.Title;

                            break;
                        }

                    case FeedbackTargetType.SUMMARY:
                        {
                            projectId = request.TargetId;

                            var projectExists = await _context.Projects
                                .AnyAsync(
                                    x => x.Id == projectId,
                                    cancellationToken);

                            if (!projectExists)
                            {
                                return Response<SubmitStakeholderFeedbackResponse>.Failure(
                                    new SubmitStakeholderFeedbackResponse(),
                                    "Project not found.",
                                    StatusCodes.Status404NotFound);
                            }

                            targetTitle = "Project Overview";

                            break;
                        }

                    default:
                        return Response<SubmitStakeholderFeedbackResponse>.Failure(
                            new SubmitStakeholderFeedbackResponse(),
                            "Unsupported target type.",
                            StatusCodes.Status400BadRequest);
                }

                var invitation = await FindAuthorizedCommenterInvitationAsync(
                    projectId,
                    request.CurrentUserId,
                    author.Email,
                    cancellationToken);

                if (invitation is null)
                {
                    return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(),"You are not authorized to submit feedback for this project.",StatusCodes.Status403Forbidden);
                }

                var feedback = new Comment(
                    projectId: projectId,
                    targetType: request.TargetType,
                    targetId: request.TargetId,
                    targetTitle: targetTitle,
                    authorId: request.CurrentUserId,
                    content: request.Content.Trim());

                _context.Comments.Add(feedback);
                await _context.SaveChangesAsync(cancellationToken);

                var response = new SubmitStakeholderFeedbackResponse
                {
                    Id = feedback.Id,
                    ProjectId = feedback.ProjectId,
                    TargetType = feedback.TargetType,
                    TargetId = feedback.TargetId,
                    TargetTitle = feedback.TargetTitle,
                    Content = feedback.Content,
                    Status = feedback.Status,
                    IsRead = feedback.IsRead,
                    Author = new FeedbackAuthorDto
                    {
                        DisplayName = author.FullName ?? author.UserName,
                        Email = author.Email
                    },
                    ResolutionNote = feedback.ResolutionNote,
                    ResolvedById = feedback.ResolvedById,
                    ResolvedAt = feedback.ResolvedAt,
                    CreatedAt = feedback.CreatedAt,
                    UpdatedAt = feedback.UpdatedAt
                };

                return Response<SubmitStakeholderFeedbackResponse>.Success(response, "Feedback submitted successfully.", StatusCodes.Status201Created);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "A concurrency error occurred while submitting feedback.", StatusCodes.Status409Conflict, new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "A database error occurred while submitting feedback.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "An unexpected error occurred while submitting feedback.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Response<ListStakeholderFeedbackResponse>> ListStakeholderFeedbackAsync(ListStakeholderFeedbackRequest request, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request.PageNumber < 1)
                errors.Add("PageNumber must be greater than or equal to 1.");

            if (request.PageSize < 1 || request.PageSize > 100)
                errors.Add("PageSize must be between 1 and 100.");
            if (request.ProjectId == Guid.Empty)
                errors.Add("ProjectId is required.");

            if (errors.Any())
            {
                return Response<ListStakeholderFeedbackResponse>.Failure(
                    new ListStakeholderFeedbackResponse(),
                    "Validation failed.",
                    StatusCodes.Status400BadRequest,
                    errors);
            }

            try
            {
                var projectId = request.ProjectId;

                if (projectId == Guid.Empty)
                {
                    return Response<ListStakeholderFeedbackResponse>.Failure(new ListStakeholderFeedbackResponse(), "project context is missing.", StatusCodes.Status401Unauthorized);
                }
                if (string.IsNullOrWhiteSpace(request.AuthorId))
                {
                    return Response<ListStakeholderFeedbackResponse>.Failure(new ListStakeholderFeedbackResponse(), "Current user is not authenticated.", StatusCodes.Status401Unauthorized);
                }

                var author = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.AuthorId, cancellationToken);

                if (author is null)
                {
                    return Response<ListStakeholderFeedbackResponse>.Failure(new ListStakeholderFeedbackResponse(), "Author not found.", StatusCodes.Status404NotFound);
                }

                var invitation = await FindAuthorizedCommenterInvitationAsync(
                    projectId,
                    request.AuthorId,
                    author.Email,
                    cancellationToken);

                if (invitation is null)
                {
                    return Response<ListStakeholderFeedbackResponse>.Failure(new ListStakeholderFeedbackResponse(),"You are not authorized to access feedback for this project.",StatusCodes.Status403Forbidden);
                }

                var baseQuery = _context.Comments
                    .AsNoTracking()
                    .Include(x => x.Author)
                    .Where(x => x.ProjectId == projectId && x.AuthorId == request.AuthorId);

                if (request.Status.HasValue)
                {
                    baseQuery = baseQuery.Where(x => x.Status == request.Status.Value);
                }

                var totalCount = await baseQuery.CountAsync(cancellationToken);

                var allProjectCommentsQuery = _context.Comments
                    .AsNoTracking()
                    .Where(x => x.ProjectId == projectId &&x.AuthorId == request.AuthorId);

                var openCount = await allProjectCommentsQuery
                    .CountAsync(x => x.Status == StakeholderFeedbackStatus.OPEN, cancellationToken);

                var resolvedCount = await allProjectCommentsQuery
                    .CountAsync(x => x.Status == StakeholderFeedbackStatus.RESOLVED, cancellationToken);

                var unreadCount = await allProjectCommentsQuery
                    .CountAsync(x => !x.IsRead, cancellationToken);

                var items = await baseQuery
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(x => new SubmitStakeholderFeedbackResponse
                    {
                        Id = x.Id,
                        ProjectId = x.ProjectId,
                        TargetType = x.TargetType,
                        TargetId = x.TargetId,
                        TargetTitle = x.TargetTitle,
                        Content = x.Content,
                        Status = x.Status,
                        IsRead = x.IsRead,
                        Author = new FeedbackAuthorDto
                        {
                            DisplayName = x.Author.FullName ?? x.Author.UserName,
                            Email = x.Author.Email
                        },
                        ResolutionNote = x.ResolutionNote,
                        ResolvedById = x.ResolvedById,
                        ResolvedAt = x.ResolvedAt,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt
                    })
                    .ToListAsync(cancellationToken);

                var response = new ListStakeholderFeedbackResponse
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    OpenCount = openCount,
                    ResolvedCount = resolvedCount,
                    UnreadCount = unreadCount
                };

                return Response<ListStakeholderFeedbackResponse>.Success(response, "Feedback retrieved successfully.", StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Response<ListStakeholderFeedbackResponse>.Failure(new ListStakeholderFeedbackResponse(), "An unexpected error occurred while retrieving feedback.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Response<SubmitStakeholderFeedbackResponse>> UpdateStakeholderFeedbackStatusAsync(UpdateStakeholderFeedbackStatusRequest request, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request.ProjectId == Guid.Empty)
                errors.Add("ProjectId is required.");

            if (request.FeedbackId == Guid.Empty)
                errors.Add("FeedbackId is required.");

            if (request.Status.HasValue &&
                !Enum.IsDefined(typeof(StakeholderFeedbackStatus), request.Status.Value))
                errors.Add("Status is invalid.");

            if (!request.Status.HasValue && !request.IsRead.HasValue)
                errors.Add("Status or IsRead is required.");

            if (!string.IsNullOrWhiteSpace(request.ResolutionNote) && request.ResolutionNote.Length > 2000)
                errors.Add("ResolutionNote must not exceed 2000 characters.");

            if (errors.Any())
            {
                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "Validation failed.", StatusCodes.Status400BadRequest, errors);
            }

            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "Current user is not authenticated.", StatusCodes.Status401Unauthorized);
            }

            try
            {
                var isAuthorized = await IsProjectFeedbackManagerAsync(
                    request.ProjectId,
                    request.UserId,
                    cancellationToken);

                if (!isAuthorized)
                {
                    return Response<SubmitStakeholderFeedbackResponse>.Failure(
                        new SubmitStakeholderFeedbackResponse(),
                        "You are not authorized to update feedback for this project.",
                        StatusCodes.Status403Forbidden);
                }

                var feedback = await _context.Comments
                    .Include(x => x.Author)
                    .FirstOrDefaultAsync(x => x.Id == request.FeedbackId && x.ProjectId == request.ProjectId, cancellationToken);

                if (feedback is null)
                {
                    return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "Feedback not found.", StatusCodes.Status404NotFound);
                }

                if (request.IsRead.HasValue)
                {
                    if (request.IsRead.Value)
                        feedback.MarkAsRead();
                    else
                        feedback.MarkAsUnread();
                }

                if (request.Status == StakeholderFeedbackStatus.RESOLVED)
                {
                    feedback.Resolve(request.UserId, request.ResolutionNote?.Trim());
                }
                else if (request.Status == StakeholderFeedbackStatus.OPEN)
                {
                    feedback.Reopen();
                }

                await _context.SaveChangesAsync(cancellationToken);

                var response = new SubmitStakeholderFeedbackResponse
                {
                    Id = feedback.Id,
                    ProjectId = feedback.ProjectId,
                    TargetType = feedback.TargetType,
                    TargetId = feedback.TargetId,
                    TargetTitle = feedback.TargetTitle,
                    Content = feedback.Content,
                    Status = feedback.Status,
                    IsRead = feedback.IsRead,
                    Author = new FeedbackAuthorDto
                    {
                        DisplayName = feedback.Author.FullName ?? feedback.Author.UserName,
                        Email = feedback.Author.Email
                    },
                    ResolutionNote = feedback.ResolutionNote,
                    ResolvedById = feedback.ResolvedById,
                    ResolvedAt = feedback.ResolvedAt,
                    CreatedAt = feedback.CreatedAt,
                    UpdatedAt = feedback.UpdatedAt
                };

                return Response<SubmitStakeholderFeedbackResponse>.Success(response, "Feedback updated successfully.", StatusCodes.Status200OK);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "A concurrency error occurred while updating feedback.", StatusCodes.Status409Conflict, new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "A database error occurred while updating feedback.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                return Response<SubmitStakeholderFeedbackResponse>.Failure(new SubmitStakeholderFeedbackResponse(), "An unexpected error occurred while updating feedback.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Response<ListStakeholderFeedbackResponse>> ListProjectStakeholderFeedbackAsync(ListProjectStakeholderFeedbackRequest request, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request.ProjectId == Guid.Empty)
                errors.Add("ProjectId is required.");

            if (request.PageNumber < 1)
                errors.Add("PageNumber must be greater than or equal to 1.");

            if (request.PageSize < 1 || request.PageSize > 100)
                errors.Add("PageSize must be between 1 and 100.");

            if (errors.Any())
            {
                return Response<ListStakeholderFeedbackResponse>.Failure(new ListStakeholderFeedbackResponse(), "Validation failed.", StatusCodes.Status400BadRequest, errors);
            }

            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Response<ListStakeholderFeedbackResponse>.Failure(
                    new ListStakeholderFeedbackResponse(),
                    "Current user is not authenticated.",
                    StatusCodes.Status401Unauthorized);
            }

            try
            {
                var projectExists = await _context.Projects
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == request.ProjectId, cancellationToken);

                if (!projectExists)
                {
                    return Response<ListStakeholderFeedbackResponse>.Failure(new ListStakeholderFeedbackResponse(), "Project not found.", StatusCodes.Status404NotFound);
                }

                var isAuthorized = await IsProjectFeedbackManagerAsync(
                    request.ProjectId,
                    request.UserId,
                    cancellationToken);

                if (!isAuthorized)
                {
                    return Response<ListStakeholderFeedbackResponse>.Failure(
                        new ListStakeholderFeedbackResponse(),
                        "You are not authorized to access feedback for this project.",
                        StatusCodes.Status403Forbidden);
                }

                var query = _context.Comments
                    .AsNoTracking()
                    .Include(x => x.Author)
                    .Where(x => x.ProjectId == request.ProjectId);

                if (request.Status.HasValue)
                {
                    query = query.Where(x => x.Status == request.Status.Value);
                }

                if (request.TargetType.HasValue)
                {
                    query = query.Where(x => x.TargetType == request.TargetType.Value);
                }

                if (request.IsRead.HasValue)
                {
                    query = query.Where(x => x.IsRead == request.IsRead.Value);
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var projectCommentsQuery = _context.Comments
                    .AsNoTracking()
                    .Where(x => x.ProjectId == request.ProjectId);

                var openCount = await projectCommentsQuery
                    .CountAsync(x => x.Status == StakeholderFeedbackStatus.OPEN, cancellationToken);

                var resolvedCount = await projectCommentsQuery
                    .CountAsync(x => x.Status == StakeholderFeedbackStatus.RESOLVED, cancellationToken);

                var unreadCount = await projectCommentsQuery
                    .CountAsync(x => !x.IsRead, cancellationToken);

                var items = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(x => new SubmitStakeholderFeedbackResponse
                    {
                        Id = x.Id,
                        ProjectId = x.ProjectId,
                        TargetType = x.TargetType,
                        TargetId = x.TargetId,
                        TargetTitle = x.TargetTitle,
                        Content = x.Content,
                        Status = x.Status,
                        IsRead = x.IsRead,
                        Author = new FeedbackAuthorDto
                        {
                            DisplayName = x.Author.FullName ?? x.Author.UserName,
                            Email = x.Author.Email
                        },
                        ResolutionNote = x.ResolutionNote,
                        ResolvedById = x.ResolvedById,
                        ResolvedAt = x.ResolvedAt,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt
                    })
                    .ToListAsync(cancellationToken);

                var response = new ListStakeholderFeedbackResponse
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    OpenCount = openCount,
                    ResolvedCount = resolvedCount,
                    UnreadCount = unreadCount
                };

                return Response<ListStakeholderFeedbackResponse>.Success(response, "Feedback retrieved successfully.", StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Response<ListStakeholderFeedbackResponse>.Failure(new ListStakeholderFeedbackResponse(), "An unexpected error occurred while retrieving project feedback.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }




        //needs refactring later, Tokenservice,...etc
        public async Task<Response<List<ProjectReviewInvitationDto>>> CreateInvitationAsync(Guid projectId, CreateProjectReviewInvitationRequest request, string userId)

        {
            try
            {
                var validation = await validator.ValidateAsync(request);

                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                    return Response<List<ProjectReviewInvitationDto>>.Failure(new List<ProjectReviewInvitationDto>(), "Validation failed", 422, errors);
                }
                if (string.IsNullOrWhiteSpace(projectId.ToString()))
                    return Response<List<ProjectReviewInvitationDto>>.Failure(new List<ProjectReviewInvitationDto>(), "Invalid projectId", 422);
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                    return Response<List<ProjectReviewInvitationDto>>.Failure(new List<ProjectReviewInvitationDto>(), "Project not found", 404);

                if (string.IsNullOrWhiteSpace(userId))
                    return Response<List<ProjectReviewInvitationDto>>.Failure(new List<ProjectReviewInvitationDto>(), "Invalid userId", 422);

                var UserInProject = await _context.ProjectMembers.Include(p => p.User).FirstOrDefaultAsync(pu => pu.ProjectId == projectId && pu.UserId == userId);

                if (UserInProject == null)
                    return Response<List<ProjectReviewInvitationDto>>.Failure(new List<ProjectReviewInvitationDto>(), "User is not a member of this project", 403);



                var invitations = new List<ProjectReviewInvitation>();

                if (request.StakeholderIds?.Any() == true)
                {
                    var existing = await _context.Users
                        .Where(s => request.StakeholderIds.Contains(s.Id))
                        .ToListAsync();

                    if (!existing.Any())
                    {
                        return Response<List<ProjectReviewInvitationDto>>.Failure("one or more stakeholder Ids doesn't belong to this project", 422);
                    }

                    foreach (var stakeholder in existing)
                    {
                        var existingPending = await _context.ProjectReviewInvitations.FirstOrDefaultAsync(i =>
                                  i.ProjectId == projectId &&
                                  i.Email == stakeholder.Email &&
                                  i.Status == InvitationStatus.Pending &&
                                  i.ExpiresAt > DateTime.UtcNow);
                        if (existingPending != null)
                        {
                            return Response<List<ProjectReviewInvitationDto>>.Failure(
                                $"Invitation already pending for {stakeholder.Email}. You can resend it.",
                                409
                            );
                        }
                        var (rawToken, hashedToken) = GenerateToken();
                        var reviewUrl = BuildProjectReviewUrl(request.Platform, rawToken);
                        invitations.Add(new ProjectReviewInvitation
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = projectId,
                            StakeholderId = stakeholder.Id,
                            Email = stakeholder.Email,
                            DisplayName = stakeholder.FullName,
                            Permission = request.Permission,
                            ReviewToken = hashedToken,
                            Status = InvitationStatus.Pending,
                            ReviewUrl =reviewUrl,//Url will be edited later 
                            ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddHours(24),
                            InvitedById = userId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                        );
                    }
                }

                // New stakeholders
                if (request.Stakeholders?.Any() == true)
                {
                    foreach (var s in request.Stakeholders)
                    {
                        //var stakeholder = new ApplicationUser(s.Email, s.Email, s.DisplayName)
                        //{
                        //    Role = UserRole.Stakeholder,
                        //};
                        //_context.Users.Add(stakeholder);
                        var existingPending = await _context.ProjectReviewInvitations.FirstOrDefaultAsync(i =>
                                     i.ProjectId == projectId &&
                                     i.Email == s.Email &&
                                     i.Status == InvitationStatus.Pending &&
                                     i.ExpiresAt > DateTime.UtcNow);
                        if (existingPending != null)
                        {
                            return Response<List<ProjectReviewInvitationDto>>.Failure(
                                $"Invitation already pending for {s.Email}. You can resend it.",
                                409
                            );
                        }

                        var (rawToken, hashedToken) = GenerateToken();
                        var reviewUrl = BuildProjectReviewUrl(request.Platform, rawToken);
                        invitations.Add(new ProjectReviewInvitation
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = projectId,
                            StakeholderId = null,
                            Email = s.Email,
                            DisplayName = s.DisplayName,
                            RoleTitle = s.RoleTitle,
                            Company = s.Company,
                            Permission = request.Permission,
                            ReviewToken = hashedToken,
                            Status = InvitationStatus.Pending,
                            ReviewUrl = reviewUrl,//Url will be edited later
                            ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddHours(24),
                            InvitedById = userId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.ProjectReviewInvitations.AddRangeAsync(invitations);
                await _context.SaveChangesAsync();

                // Send Emails
                foreach (var inv in invitations)
                {
                    try
                    {
                        var subject = "You're invited to review a project";
                        var body = ProjectReviewInvitationTemplate.ProjectReviewInvitationEmail(
                         inv.DisplayName,project.Name,request.Permission.ToString(), request.ExpiresAt,
                         inv.ReviewUrl,UserInProject.User.FullName);

                        await emailSender.SendEmailAsync(inv.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to send email to {Email}", inv.Email);
                        return Response<List<ProjectReviewInvitationDto>>.Failure($"Failed to send email to {inv.Email}: {ex.Message}", 500);
                    }
                }

                var result = invitations.Select(inv => new ProjectReviewInvitationDto
                {
                    Id = inv.Id,
                    ProjectId = inv.ProjectId,
                    StakeholderId = inv.StakeholderId,
                    Email = inv.Email,
                    DisplayName = inv.DisplayName,
                    RoleTitle = inv.RoleTitle,
                    Company = inv.Company,
                    Permission = inv.Permission,
                    Status = inv.Status,
                    ReviewUrl = inv.ReviewUrl,
                    ExpiresAt = inv.ExpiresAt,
                    CreatedAt = inv.CreatedAt,
                    UpdatedAt = inv.UpdatedAt,
                    InvitedById = inv.InvitedById
                }).ToList();

                return Response<List<ProjectReviewInvitationDto>>.Success(
                    result,
                    "Stakeholder review invitation created successfully.",
                    201);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating project review invitations.");
                return Response<List<ProjectReviewInvitationDto>>.Failure("An error occurred while creating project review invitations.", 500, new List<string> { ex.Message });
            }


        }
        public async Task<Response<ProjectReviewInvitationsPagedResult<ProjectReviewInvitationDto>>>GetProjectReviewInvitationsAsync(Guid projectId, GetProjectReviewInvitationsQuery query, string userId)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(projectId.ToString()))
                    return Response<ProjectReviewInvitationsPagedResult<ProjectReviewInvitationDto>>.Failure("Invalid projectId", 422);
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                    return Response<ProjectReviewInvitationsPagedResult<ProjectReviewInvitationDto>>.Failure("Project not found", 404);

                if (string.IsNullOrWhiteSpace(userId))
                    return Response<ProjectReviewInvitationsPagedResult<ProjectReviewInvitationDto>>.Failure("Invalid userId", 422);

                var UserInProject = await _context.ProjectMembers.Include(p => p.User).FirstOrDefaultAsync(pu => pu.ProjectId == projectId && pu.UserId == userId);

                if (UserInProject == null)
                    return Response<ProjectReviewInvitationsPagedResult<ProjectReviewInvitationDto>>.Failure("User is not a member of this project", 403);
                var baseQuery = _context.ProjectReviewInvitations
                .Where(i => i.ProjectId == projectId);

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var search = query.Search.ToLower();

                    baseQuery = baseQuery.Where(i =>
                        i.Email.ToLower().Contains(search) ||
                        i.DisplayName.ToLower().Contains(search));
                }

                if (query.Status.HasValue)
                {
                    baseQuery = baseQuery.Where(i => i.Status == query.Status);
                }

                var pendingCount = await baseQuery.CountAsync(i => i.Status == InvitationStatus.Pending);
                var acceptedCount = await baseQuery.CountAsync(i => i.Status == InvitationStatus.Accepted);
                var revokedCount = await baseQuery.CountAsync(i => i.Status == InvitationStatus.Revoked);


                baseQuery = baseQuery.Select(i => new ProjectReviewInvitation
                {
                    Id = i.Id,
                    ProjectId = i.ProjectId,
                    StakeholderId = i.StakeholderId,
                    Email = i.Email,
                    DisplayName = i.DisplayName,
                    Permission = i.Permission,
                    Status = (i.Status == InvitationStatus.Pending && i.ExpiresAt <= DateTime.UtcNow)
                                ? InvitationStatus.Expired
                                : i.Status,
                    ReviewUrl = i.ReviewUrl,
                    ExpiresAt = i.ExpiresAt,
                    AcceptedAt = i.AcceptedAt,
                    RevokedAt = i.RevokedAt,
                    InvitedById = i.InvitedById,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                });

                var totalCount = await baseQuery.CountAsync();

                var items = await baseQuery
                    .OrderByDescending(i => i.CreatedAt)
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(inv => new ProjectReviewInvitationDto
                    {
                        Id = inv.Id,
                        ProjectId = inv.ProjectId,
                        StakeholderId = inv.StakeholderId,
                        Email = inv.Email,
                        DisplayName = inv.DisplayName,
                        Permission = inv.Permission,
                        Status = inv.Status,
                        ReviewUrl = inv.ReviewUrl,
                        ExpiresAt = inv.ExpiresAt,
                        AcceptedAt = inv.AcceptedAt,
                        RevokedAt = inv.RevokedAt,
                        InvitedById = inv.InvitedById,
                        CreatedAt = inv.CreatedAt,
                        UpdatedAt = inv.UpdatedAt
                    })
                    .ToListAsync();

                var result = new ProjectReviewInvitationsPagedResult<ProjectReviewInvitationDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    PendingCount = pendingCount,
                    AcceptedCount = acceptedCount,
                    RevokedCount = revokedCount
                };

                return Response<ProjectReviewInvitationsPagedResult<ProjectReviewInvitationDto>>
                    .Success(result, "Project review invitations retrieved successfully.", 200);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving project review invitations.");
                return Response<ProjectReviewInvitationsPagedResult<ProjectReviewInvitationDto>>
                    .Failure("An error occurred while retrieving project review invitations.", 500, new List<string> { ex.Message });
            }
        }
        public async Task<Response<ProjectReviewInvitationDto>> ResendInvitationAsync(Guid projectId, Guid invitationId, string ResendByUserId)
        {

            try
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                if (project == null)
                    return Response<ProjectReviewInvitationDto>.Failure("Project not found.", 404);
                if (string.IsNullOrWhiteSpace(ResendByUserId))
                    return Response<ProjectReviewInvitationDto>.Failure("Validation failed", 422, new List<string> { "Invalid userId" });

                var UserInProject = await _context.ProjectMembers.Include(p => p.User).FirstOrDefaultAsync(pu => pu.ProjectId == projectId && pu.UserId == ResendByUserId);

                if (UserInProject == null)
                    return Response<ProjectReviewInvitationDto>.Failure("User is not a member of this project", 403);

                var invitation = await _context.ProjectReviewInvitations
                    .FirstOrDefaultAsync(i =>
                        i.Id == invitationId &&
                        i.ProjectId == projectId);

                if (invitation == null)
                    return Response<ProjectReviewInvitationDto>.Failure("Invitation not found.", 404);


                if (invitation.Status == InvitationStatus.Pending && invitation.ExpiresAt <= DateTime.UtcNow)
                {
                    invitation.Status = InvitationStatus.Expired;
                    _context.ProjectReviewInvitations.Update(invitation);
                    await _context.SaveChangesAsync();

                }

                if (invitation.Status == InvitationStatus.Expired || invitation.Status == InvitationStatus.Revoked)
                    return Response<ProjectReviewInvitationDto>.Failure("Only pending invitations can be resent.", 409);

                if (invitation.Status == InvitationStatus.Accepted)
                    return Response<ProjectReviewInvitationDto>.Failure("The invitation has already been accepted.", 409);


                //will be refactored as service later
                var (rawToken, hashedToken) = GenerateToken();

                var newReviewUrl = $"http://localhost:5173/project-review/{rawToken}";

                invitation.UpdateProjectReviewInvitation(
                    hashedToken,
                    newReviewUrl,
                   DateTime.UtcNow.AddHours(24)
                );
                await _context.SaveChangesAsync();

                //may be refactored as service later
                try
                {
                    var subject = "You're invited to review a project";
                    var body = ProjectReviewInvitationTemplate.ProjectReviewInvitationEmail(
                     invitation.DisplayName, project.Name, invitation.Permission.ToString(), invitation.ExpiresAt,
                     invitation.ReviewUrl, UserInProject.User.FullName);

                    await emailSender.SendEmailAsync(invitation.Email, subject, body);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send email to {Email}", invitation.Email);
                    return Response<ProjectReviewInvitationDto>.Failure($"Failed to send email to {invitation.Email}: {ex.Message}", 500);
                }

                var dto = new ProjectReviewInvitationDto
                {
                    Id = invitation.Id,
                    ProjectId = invitation.ProjectId,
                    StakeholderId = invitation.StakeholderId?.ToString(),
                    Email = invitation.Email,
                    DisplayName = invitation.DisplayName,
                    Company = invitation.Company,
                    RoleTitle = invitation.RoleTitle,
                    Permission = invitation.Permission,
                    Status = invitation.Status,
                    ReviewUrl = invitation.ReviewUrl,
                    ExpiresAt = invitation.ExpiresAt,
                    AcceptedAt = invitation.AcceptedAt,
                    RevokedAt = invitation.RevokedAt,
                    InvitedById = invitation.InvitedById,
                    CreatedAt = invitation.CreatedAt,
                    UpdatedAt = invitation.UpdatedAt
                };

                return Response<ProjectReviewInvitationDto>.Success(
                    dto,
                    "Project review invitation updated successfully.",
                    200);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while resending project review invitation.");
                return Response<ProjectReviewInvitationDto>.Failure("An error occurred while resending project review invitation.", 500, new List<string> { ex.Message });
            }
        }

        public async Task<Response<RevokeInvitationResponseDto>> RevokeInvitationAsync(Guid projectId, Guid invitationId, string userId)
        {

            try
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                if (project == null)
                    return Response<RevokeInvitationResponseDto>.Failure("Project not found.", 404);
                if (string.IsNullOrWhiteSpace(userId))
                    return Response<RevokeInvitationResponseDto>.Failure("Validation failed", 422, new List<string> { "Invalid userId" });

                //should apply later which role can revoke invitation PM,BA...etc
                var UserInProject = await _context.ProjectMembers.Include(p => p.User).FirstOrDefaultAsync(pu => pu.ProjectId == projectId && pu.UserId == userId);

                if (UserInProject == null)
                    return Response<RevokeInvitationResponseDto>.Failure("User is not a member of this project, Don't have permission to revoke invitation", 403);
                var invitation = await _context.ProjectReviewInvitations
                    .FirstOrDefaultAsync(i =>
                        i.Id == invitationId &&
                        i.ProjectId == projectId);

                if (invitation == null)
                    return Response<RevokeInvitationResponseDto>.Failure("Invitation not found.", 404);

                var now = DateTime.UtcNow;

                if (invitation.Status == InvitationStatus.Pending && invitation.ExpiresAt <= now)
                {
                    invitation.Status = InvitationStatus.Expired;
                    await _context.SaveChangesAsync();
                }

                if (invitation.Status == InvitationStatus.Accepted)
                {
                    return Response<RevokeInvitationResponseDto>.Failure("Accepted invitations cannot be revoked.", 409);
                }

                // Idempotent behavior 
                if (invitation.Status == InvitationStatus.Revoked)
                {
                    return Response<RevokeInvitationResponseDto>.Success(
                        new RevokeInvitationResponseDto
                        {
                            Id = invitation.Id.ToString(),
                            Status = invitation.Status
                        },
                        "Project review invitation revoked successfully.",
                        200);
                }

                invitation.Revoke();
                await _context.SaveChangesAsync();

                var dto = new RevokeInvitationResponseDto
                {
                    Id = invitation.Id.ToString(),
                    Status = invitation.Status
                };

                return Response<RevokeInvitationResponseDto>.Success(
                    dto,
                    "Project review invitation revoked successfully.",
                    200);
            }
            catch(Exception ex) {
                logger.LogError(ex, "An error occurred while revoking project review invitation.");
                return Response<RevokeInvitationResponseDto>.Failure("An error occurred while revoking project review invitation.", 500, new List<string> { ex.Message });
            }
        }




        public async Task<Response<PreviewProjectReviewInvitationResponse>> PreviewProjectReviewInvitationAsync(PreviewProjectReviewInvitationRequest request,CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request == null || string.IsNullOrWhiteSpace(request.Token))
                errors.Add("Token is required.");

            if (errors.Any())
            {
                return Response<PreviewProjectReviewInvitationResponse>.Failure(new PreviewProjectReviewInvitationResponse(),"Validation failed.",StatusCodes.Status400BadRequest,errors);
            }

            try
            {

                var hashedToken = HashToken(request.Token);

                var invitation = await _context.ProjectReviewInvitations.AsNoTracking().FirstOrDefaultAsync( x => x.ReviewToken == hashedToken, cancellationToken);

                if (invitation == null)
                {
                    return Response<PreviewProjectReviewInvitationResponse>.Failure(new PreviewProjectReviewInvitationResponse(),"Invitation not found.",StatusCodes.Status404NotFound);
                }

                if (invitation.Status == InvitationStatus.Revoked || invitation.RevokedAt.HasValue)
                {
                    return Response<PreviewProjectReviewInvitationResponse>.Failure(
                        new PreviewProjectReviewInvitationResponse
                        {
                            ProjectId = invitation.ProjectId,
                            StakeholderEmail = invitation.Email,
                            StakeholderDisplayName = invitation.DisplayName,
                            Permission = invitation.Permission,
                            Status = InvitationStatus.Revoked,
                            ExpiresAt = invitation.ExpiresAt,
                            AcceptedAt = invitation.AcceptedAt
                        },
                        "Invitation has been revoked.",
                        StatusCodes.Status409Conflict);
                }

                if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return Response<PreviewProjectReviewInvitationResponse>.Failure(
                        new PreviewProjectReviewInvitationResponse
                        {
                            ProjectId = invitation.ProjectId,
                            StakeholderEmail = invitation.Email,
                            StakeholderDisplayName = invitation.DisplayName,
                            Permission = invitation.Permission,
                            Status = InvitationStatus.Expired,
                            ExpiresAt = invitation.ExpiresAt,
                            AcceptedAt = invitation.AcceptedAt
                        },
                        "Invitation has expired.",
                        StatusCodes.Status409Conflict);
                }

                var project = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == invitation.ProjectId, cancellationToken);

                if (project == null)
                {
                    return Response<PreviewProjectReviewInvitationResponse>.Failure(new PreviewProjectReviewInvitationResponse(),"Project not found.",StatusCodes.Status404NotFound);
                }

                var response = new PreviewProjectReviewInvitationResponse
                {
                    ProjectId = invitation.ProjectId,
                    ProjectName = project.Name,
                    StakeholderEmail = invitation.Email,
                    StakeholderDisplayName = invitation.DisplayName,
                    Permission = invitation.Permission,
                    Status = invitation.Status,
                    ExpiresAt = invitation.ExpiresAt,
                    AcceptedAt = invitation.AcceptedAt
                };

                return Response<PreviewProjectReviewInvitationResponse>.Success(response,"Invitation preview retrieved successfully.",StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Response<PreviewProjectReviewInvitationResponse>.Failure(
                    new PreviewProjectReviewInvitationResponse(),
                    "An unexpected error occurred while retrieving the invitation preview.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }
        public async Task<Response<AcceptProjectReviewInvitationResponse>> AcceptProjectReviewInvitationAsync(AcceptProjectReviewInvitationRequest request,CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request == null || string.IsNullOrWhiteSpace(request.Token))
                errors.Add("Token is required.");

            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                if (request.DisplayName.Trim().Length < 2)
                    errors.Add("DisplayName must be at least 2 characters.");

                if (request.DisplayName.Trim().Length > 120)
                    errors.Add("DisplayName must not exceed 120 characters.");
            }

            if (errors.Any())
            {
                return Response<AcceptProjectReviewInvitationResponse>.Failure(new AcceptProjectReviewInvitationResponse(),"Validation failed.",StatusCodes.Status400BadRequest,errors);
            }

            try
            {
                var hashedToken = HashToken(request.Token);

                var invitation = await _context.ProjectReviewInvitations.FirstOrDefaultAsync(x => x.ReviewToken == hashedToken,cancellationToken);

                if (invitation == null)
                {
                    return Response<AcceptProjectReviewInvitationResponse>.Failure(new AcceptProjectReviewInvitationResponse(), $"Invitation not found.", StatusCodes.Status404NotFound);
                }

                if (invitation.Status == InvitationStatus.Revoked || invitation.RevokedAt.HasValue)
                {
                    return Response<AcceptProjectReviewInvitationResponse>.Failure(
                        new AcceptProjectReviewInvitationResponse
                        {
                            ProjectId = invitation.ProjectId,
                            AccessId = invitation.ReviewToken,
                            Permission = invitation.Permission,
                            Status = InvitationStatus.Revoked,
                            AcceptedAt = invitation.AcceptedAt
                        },
                        "Invitation has been revoked.",
                        StatusCodes.Status409Conflict);
                }

                if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
                {
                    invitation.Status = InvitationStatus.Expired;
                    invitation.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    return Response<AcceptProjectReviewInvitationResponse>.Failure(
                        new AcceptProjectReviewInvitationResponse
                        {
                            ProjectId = invitation.ProjectId,
                            AccessId = invitation.ReviewToken,
                            Permission = invitation.Permission,
                            Status = InvitationStatus.Expired ,
                            AcceptedAt = invitation.AcceptedAt
                        },
                        "Invitation has expired.",
                        StatusCodes.Status409Conflict);
                }

                if (invitation.Status == InvitationStatus.Accepted)
                {
                    var alreadyAcceptedResponse = new AcceptProjectReviewInvitationResponse
                    {
                        ProjectId = invitation.ProjectId,
                        AccessId = invitation.ReviewToken,
                        Permission = invitation.Permission,
                        Status = InvitationStatus.Accepted,
                        AcceptedAt = invitation.AcceptedAt
                    };

                    return Response<AcceptProjectReviewInvitationResponse>.Success(alreadyAcceptedResponse,"Invitation already accepted.",StatusCodes.Status200OK);
                }

                invitation.Accept(request.DisplayName?.Trim());

                await _context.SaveChangesAsync(cancellationToken);

                var response = new AcceptProjectReviewInvitationResponse
                {
                    ProjectId = invitation.ProjectId,
                    AccessId = invitation.ReviewToken,
                    Permission = invitation.Permission,
                    Status = InvitationStatus.Accepted,
                    AcceptedAt = invitation.AcceptedAt
                };

                return Response<AcceptProjectReviewInvitationResponse>.Success(response,"Invitation accepted successfully.",
                    StatusCodes.Status200OK);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<AcceptProjectReviewInvitationResponse>.Failure(new AcceptProjectReviewInvitationResponse(),"A concurrency error occurred while accepting the invitation.",StatusCodes.Status409Conflict,new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<AcceptProjectReviewInvitationResponse>.Failure(new AcceptProjectReviewInvitationResponse(),"A database error occurred while accepting the invitation.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                return Response<AcceptProjectReviewInvitationResponse>.Failure(new AcceptProjectReviewInvitationResponse(),"An unexpected error occurred while accepting the invitation.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
        }
        public async Task<Response<GetProjectReviewDashboardResponse>> GetProjectReviewDashboardAsync(GetProjectReviewDashboardRequest request,CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request == null || string.IsNullOrWhiteSpace(request.Token))
                errors.Add("Token is required.");

            if (errors.Any())
            {
                return Response<GetProjectReviewDashboardResponse>.Failure(new GetProjectReviewDashboardResponse(),"Validation failed.",StatusCodes.Status400BadRequest,errors);
            }

            try
            {
                var hashedToken = HashToken(request.Token);

                var invitation = await _context.ProjectReviewInvitations.AsNoTracking().FirstOrDefaultAsync(x => x.ReviewToken == hashedToken,cancellationToken);

                if (invitation == null)
                {
                    return Response<GetProjectReviewDashboardResponse>.Failure(new GetProjectReviewDashboardResponse(),$"Invitation not found.",StatusCodes.Status404NotFound);
                }

                if (invitation.Status == InvitationStatus.Revoked || invitation.RevokedAt.HasValue)
                {
                    return Response<GetProjectReviewDashboardResponse>.Failure(new GetProjectReviewDashboardResponse(),"Invitation has been revoked.",StatusCodes.Status409Conflict);
                }

                if (invitation.Status == InvitationStatus.Expired ||
                    (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow))
                {
                    return Response<GetProjectReviewDashboardResponse>.Failure(new GetProjectReviewDashboardResponse(),"Invitation has expired.",StatusCodes.Status409Conflict);
                }

                if (invitation.Status != InvitationStatus.Accepted)
                {
                    return Response<GetProjectReviewDashboardResponse>.Failure(new GetProjectReviewDashboardResponse(),"Invitation must be accepted before accessing the dashboard.",StatusCodes.Status403Forbidden);
                }

                var project = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == invitation.ProjectId, cancellationToken);

                if (project == null)
                {
                    return Response<GetProjectReviewDashboardResponse>.Failure(new GetProjectReviewDashboardResponse(),"Project not found.",StatusCodes.Status404NotFound);
                }

                var analysisRun = await _context.AnalysisRuns
                    .AsNoTracking()
                    .Where(x => x.ProjectId== invitation.ProjectId && (x.Status == AnalysisRunStatus.COMPLETED|| x.Status == AnalysisRunStatus.PARTIAL))
                    .OrderByDescending(x => x.CompletedAt ?? x.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (analysisRun == null)
                {
                    return Response<GetProjectReviewDashboardResponse>.Failure(new GetProjectReviewDashboardResponse(),"No completed analysis run found for this project.",StatusCodes.Status404NotFound);
                }

                var analysisResult = await _context.AnalysisResults
                    .AsNoTracking()
                    .Where(x => x.AnalysisRunId == analysisRun.Id)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (analysisResult == null || string.IsNullOrWhiteSpace(analysisResult.RawJson))
                {
                    return Response<GetProjectReviewDashboardResponse>.Failure(new GetProjectReviewDashboardResponse(),"Analysis result not found.",StatusCodes.Status404NotFound);
                }

                ProjectReviewDashboardRawResultDto? rawResult;

                try
                {
                    rawResult = JsonSerializer.Deserialize<ProjectReviewDashboardRawResultDto>(
                        analysisResult.RawJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                }
                catch (Exception ex)
                {
                    return Response<GetProjectReviewDashboardResponse>.Failure(new GetProjectReviewDashboardResponse(),"Failed to parse analysis result JSON.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
                }

                if (rawResult == null)
                {
                    return Response<GetProjectReviewDashboardResponse>.Failure(new GetProjectReviewDashboardResponse(),"Analysis result payload is empty or invalid.",StatusCodes.Status500InternalServerError);
                }
                
                var requirementEntities = await _context.Requirements
                    .AsNoTracking()
                    .Where(x => x.ProjectId == invitation.ProjectId)
                    .Select(x => new
                    {
                        x.Id,
                        x.SourceRequirementId,
                        x.Title,
                        x.Description,
                        x.Type,
                        x.Priority,
                        x.ConfidenceScore
                    })
                    .ToListAsync(cancellationToken);

                var requirementMap = requirementEntities
                    .Where(x => !string.IsNullOrWhiteSpace(x.SourceRequirementId))
                    .GroupBy(x => x.SourceRequirementId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(x => x.Id).First());

           
                var userStoryEntities = await _context.UserStories
                    .AsNoTracking()
                    .Where(x => x.ProjectId == invitation.ProjectId)
                    .Select(x => new
                    {
                        x.Id,
                        x.SourceUserStoryId,   
                        x.Title,
                        x.Description,
                        //x.UserStory,
                        x.Priority,
                        x.RequirementId
                    })
                    .ToListAsync(cancellationToken);

                var userStoryMap = userStoryEntities
                    .Where(x => !string.IsNullOrWhiteSpace(x.SourceUserStoryId))
                    .GroupBy(x => x.SourceUserStoryId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(x => x.Id).First());


                var response = new GetProjectReviewDashboardResponse
                {
                    ProjectId = invitation.ProjectId,
                    ProjectName = project.Name,
                    AnalysisRunId = analysisRun.Id,
                    GeneratedAt = analysisResult.CreatedAt,
                    Permission = invitation.Permission,
                    Summary = new ProjectReviewDashboardSummaryDto
                    {
                        ExecutiveSummary = rawResult.Summary?.ExecutiveSummary,
                        KeyDecisions = rawResult.Summary?.KeyDecisions ?? new List<string>(),
                        OpenQuestions = rawResult.Summary?.OpenQuestions ?? new List<string>(),
                        Risks = rawResult.Summary?.Risks ?? new List<string>(),
                        Assumptions = rawResult.Summary?.Assumptions ?? new List<string>(),
                        Scope = rawResult.Summary?.Scope ?? new List<string>(),
                        OutOfScope = rawResult.Summary?.OutOfScope ?? new List<string>()
                    },
                    //Requirements = rawResult.Requirements?
                    //    .Select(x => new ProjectReviewDashboardRequirementDto
                    //    {
                    //        Id = x.Id,
                    //        RequirementId = x.Id,
                    //        Title = x.Title,
                    //        Description = x.Description,
                    //        Classification = x.Type,
                    //        Priority = x.Priority,
                    //        ConfidenceScore = x.ConfidenceScore
                    //    })
                    //    .ToList() ?? new List<ProjectReviewDashboardRequirementDto>(),
                    //UserStories = rawResult.UserStories?
                    //    .Select(x => new ProjectReviewDashboardUserStoryDto
                    //    {
                    //        Id = x.Id,
                    //        StoryId = x.Id,
                    //        Title = x.Title,
                    //        Description = x.Title,
                    //        UserStory = x.UserStory,
                    //        AcceptanceCriteria = x.AcceptanceCriteria?
                    //            .Select(ac => ac.Text)
                    //            .ToList() ?? new List<string>(),
                    //        Priority = x.Priority,
                    //        RequirementId = x.RequirementId,
                    //        Classification = x.Type
                    //    })
                    //    .ToList() ?? new List<ProjectReviewDashboardUserStoryDto>()
                    Requirements = rawResult.Requirements?
                .Select(x =>
                {
                    requirementMap.TryGetValue(x.Id, out var matchedRequirement);

                    return new ProjectReviewDashboardRequirementDto
                    {
                        Id = x.Id, // REQ-001
                        FeedbackTargetId = matchedRequirement?.Id ?? Guid.Empty,
                        RequirementId = x.Id,
                        Title = x.Title,
                        Description = x.Description,
                        Classification = x.Type,
                        Priority = x.Priority,
                        ConfidenceScore = x.ConfidenceScore
                    };
                })
                .ToList() ?? new List<ProjectReviewDashboardRequirementDto>(),

                    UserStories = rawResult.UserStories?
                .Select(x =>
                {
                    userStoryMap.TryGetValue(x.Id, out var matchedUserStory);

                    return new ProjectReviewDashboardUserStoryDto
                    {
                        Id = x.Id, // US-001
                        FeedbackTargetId = matchedUserStory?.Id ?? Guid.Empty,
                        StoryId = x.Id,
                        Title = x.Title,
                        Description = x.Title, 
                        UserStory = x.UserStory,
                        AcceptanceCriteria = x.AcceptanceCriteria?
                            .Select(ac => ac.Text)
                            .ToList() ?? new List<string>(),
                        Priority = x.Priority,
                        RequirementId = x.RequirementId,
                        Classification = x.Type
                    };
                })
                .ToList() ?? new List<ProjectReviewDashboardUserStoryDto>()
                };

                return Response<GetProjectReviewDashboardResponse>.Success(response,"Project review dashboard retrieved successfully.",StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Response<GetProjectReviewDashboardResponse>.Failure(new GetProjectReviewDashboardResponse(),"An unexpected error occurred while retrieving the project review dashboard.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
        }

        private static (string RawToken, string HashedToken) GenerateToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(32);

            var rawToken = WebEncoders.Base64UrlEncode(tokenBytes);

            var hashedToken = WebEncoders.Base64UrlEncode(
                SHA256.HashData(tokenBytes)
            );

            return (rawToken, hashedToken);
        }

        private static string HashToken(string rawToken)
        {
            var tokenBytes = WebEncoders.Base64UrlDecode(rawToken);

            return WebEncoders.Base64UrlEncode(
                SHA256.HashData(tokenBytes)
            );
        }

        private async Task<ProjectReviewInvitation?> FindAuthorizedCommenterInvitationAsync(
            Guid projectId,
            string currentUserId,
            string? currentUserEmail,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var candidates = await _context.ProjectReviewInvitations
                .Where(x =>
                    x.ProjectId == projectId &&
                    x.Status == InvitationStatus.Accepted &&
                    x.Permission == ProjectReviewPermission.COMMENTER &&
                    (!x.ExpiresAt.HasValue || x.ExpiresAt.Value > now))
                .OrderByDescending(x => x.AcceptedAt ?? x.CreatedAt)
                .ToListAsync(cancellationToken);

            var invitation = candidates.FirstOrDefault(x => x.StakeholderId == currentUserId);
            if (invitation is not null)
            {
                return invitation;
            }

            if (string.IsNullOrWhiteSpace(currentUserEmail))
            {
                return null;
            }

            invitation = candidates.FirstOrDefault(x =>
                string.Equals(
                    x.Email.Trim(),
                    currentUserEmail.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (invitation is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(invitation.StakeholderId))
            {
                var belongsToExistingUser = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == invitation.StakeholderId, cancellationToken);

                if (belongsToExistingUser)
                {
                    return null;
                }
            }

            invitation.StakeholderId = currentUserId;
            invitation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return invitation;
        }

        private Task<bool> IsProjectFeedbackManagerAsync(
            Guid projectId,
            string userId,
            CancellationToken cancellationToken)
        {
            return _context.ProjectMembers
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.ProjectId == projectId &&
                        x.UserId == userId &&
                        (x.Role == ProjectRole.Owner || x.Role == ProjectRole.Contributor),
                    cancellationToken);
        }
        private string BuildProjectReviewUrl(ClientPlatform platform, string rawToken)
        {
            if (platform == ClientPlatform.Mobile)
            {
                var baseUrl = _projectReviewLinkOptions.Value.MobileBaseUrl.TrimEnd('/');

                // Deep/app-link style
                return $"https://requra-ai.runasp.net/project-review/{Uri.EscapeDataString(rawToken)}";
            }

            var webBaseUrl = _projectReviewLinkOptions.Value.WebBaseUrl.TrimEnd('/');
            return $"http://localhost:5173/project-review/{Uri.EscapeDataString(rawToken)}";
        }



    }
}
