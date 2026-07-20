using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Meeting;
using Requra.Application.DTOs.Project.ProjectResults.Feedbacks;
using Requra.Application.DTOs.ProjectReviewInvitaion;
using Requra.Application.Interfaces.IProjectService.IProjectReviewService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.IEmailSender;
using Requra.Infrastructure.ExternalServices.EmailSender;
using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Requra.Infrastructure.Services.ProjectService.ProjectReviewService
{
    public class ProjectReviewService(RequraDbContext _context, IEmailSender emailSender,
    ILogger<ProjectReviewService> logger,IValidator<CreateProjectReviewInvitationRequest> validator) : IProjectReviewService
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

                            projectId = requirement.ProjectId ?? Guid.Empty;
                            targetTitle = requirement.Title;
                            break;
                        }

                    case FeedbackTargetType.SUMMARY:
                        {
                            var summary = await _context.Summaries
                                .FirstOrDefaultAsync(x => x.Id == request.TargetId, cancellationToken);

                            if (summary is null)
                            {
                                return Response<SubmitStakeholderFeedbackResponse>.Failure(
                                    new SubmitStakeholderFeedbackResponse(),
                                    "Summary not found.",
                                    StatusCodes.Status404NotFound);
                            }

                            projectId = summary.ProjectId ?? Guid.Empty;
                            targetTitle = "Summary";
                            break;
                        }

                    default:
                        return Response<SubmitStakeholderFeedbackResponse>.Failure(
                            new SubmitStakeholderFeedbackResponse(),
                            "Unsupported target type.",
                            StatusCodes.Status400BadRequest);
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
                    .Where(x => x.ProjectId == projectId);

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

            if (!Enum.IsDefined(typeof(StakeholderFeedbackStatus), request.Status))
                errors.Add("Status is invalid.");

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

            try
            {
                var projectExists = await _context.Projects
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == request.ProjectId, cancellationToken);

                if (!projectExists)
                {
                    return Response<ListStakeholderFeedbackResponse>.Failure(new ListStakeholderFeedbackResponse(), "Project not found.", StatusCodes.Status404NotFound);
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

        public async Task<Response<List<ProjectReviewInvitationDto>>> CreateInvitationAsync(string projectId, CreateProjectReviewInvitationRequest request, string userId)

        {
            try
            {
                var validation = await validator.ValidateAsync(request);

                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                    return Response<List<ProjectReviewInvitationDto>>.Failure(null, "Validation failed", 422, errors);
                }
                if (string.IsNullOrWhiteSpace(projectId))
                    return Response<List<ProjectReviewInvitationDto>>.Failure("Invalid projectId", 422);
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id.ToString() == projectId);

                if (project == null)
                    return Response<List<ProjectReviewInvitationDto>>.Failure("Project not found", 404);

                if(string.IsNullOrWhiteSpace(userId))
                    return Response<List<ProjectReviewInvitationDto>>.Failure("Invalid userId", 422);

                var UserInProject = await _context.ProjectMembers.Include(p => p.User).FirstOrDefaultAsync(pu => pu.ProjectId.ToString() == projectId && pu.UserId == userId);

                if(UserInProject == null)
                    return Response<List<ProjectReviewInvitationDto>>.Failure("User is not a member of this project", 403);



                var invitations = new List<ProjectReviewInvitation>();

                if (request.StakeholderIds?.Any() == true)
                {
                    var existing = await _context.Users
                        .Where(s => request.StakeholderIds.Contains(s.Id))
                        .ToListAsync();

                    if(!existing.Any())
                    {
                        return Response<List<ProjectReviewInvitationDto>>.Failure("one or more stakeholder Ids doesn't belong to this project", 422);
                    }

                    foreach (var stakeholder in existing)
                    {
                        var existingPending = await _context.ProjectReviewInvitations.FirstOrDefaultAsync(i =>
                                  i.ProjectId == projectId &&
                                  i.Email == stakeholder.Email &&
                                  i.Status == "PENDING" &&
                                  i.ExpiresAt > DateTime.UtcNow);
                        if (existingPending != null)
                        {
                            return Response<List<ProjectReviewInvitationDto>>.Failure(
                                $"Invitation already pending for {stakeholder.Email}. You can resend it.",
                                409
                            );
                        }
                        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                        using var sha = SHA256.Create();
                        var hashedToken = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken)));
                        invitations.Add(new ProjectReviewInvitation
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = projectId,
                            StakeholderId = stakeholder.Id, 
                            Email = stakeholder.Email,
                            DisplayName = stakeholder.FullName,
                            Permission = request.Permission,
                            ReviewToken = hashedToken,
                            Status = "PENDING",
                            ReviewUrl = $"https://app.requra.ai/project-review/{rawToken}",//Url will be edited later 
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
                                     i.Status == "PENDING" &&
                                     i.ExpiresAt > DateTime.UtcNow);
                        if (existingPending != null)
                        {
                            return Response<List<ProjectReviewInvitationDto>>.Failure(
                                $"Invitation already pending for {s.Email}. You can resend it.",
                                409 
                            );
                        }

                        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                        using var sha = SHA256.Create();
                        var hashedToken = Convert.ToBase64String(
                            sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken))
                        );
                        invitations.Add(new ProjectReviewInvitation
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = projectId,
                            StakeholderId = Guid.NewGuid().ToString(), 
                            Email = s.Email,
                            DisplayName = s.DisplayName,
                            Permission = request.Permission,
                            ReviewToken = hashedToken,
                            Status = "PENDING",
                            ReviewUrl = $"https://app.requra.ai/project-review/{rawToken}",//Url will be edited later
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
    inv.DisplayName,
    project.Name,
    request.Permission.ToString(),
    request.ExpiresAt,
    inv.ReviewUrl,
    UserInProject.User.FullName //might need include
);

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
                    StakeholderId = inv.StakeholderId.ToString(),
                    Email = inv.Email,
                    DisplayName = inv.DisplayName,
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
                logger.LogError(ex, "An error occurred while creating project review invitations." );
                return Response<List<ProjectReviewInvitationDto>>.Failure("An error occurred while creating project review invitations.", 500, new List<string> { ex.Message });
            }


        }
    }
}
