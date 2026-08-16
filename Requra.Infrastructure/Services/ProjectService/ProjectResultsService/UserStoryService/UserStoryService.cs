using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Project.ProjectResults.UserStory;
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
        public async Task<Response<PagedResult<UserStoryDto>>> GetUserStoriesByProjectIdAsync(Guid projectId)
        {
            if (projectId == Guid.Empty)
                return Response<PagedResult<UserStoryDto>>.Failure(new PagedResult<UserStoryDto>(),"Invalid ProjectId",400);
            try
            {
                var projectRepo = _unitOfWork.Repository<Project>();
                var projectExists = await projectRepo.GetByIdAsync(projectId);

                if (projectExists == null)
                    return Response<PagedResult<UserStoryDto>>.Failure(new PagedResult<UserStoryDto>(),"Project not found",404);

                //var query = _context.UserStories.AsNoTracking().Where(us => us.ProjectId == projectId);
                //var totalCount = await query.CountAsync();
                //var items = await query.OrderByDescending(us => us.CreatedAt).ProjectTo<UserStoryDto>(_mapper.ConfigurationProvider).ToListAsync();
                var query = _context.UserStories.AsNoTracking()
                   .Include(us => us.Creator)
                   .Where(us => us.ProjectId == projectId);
                var totalCount = await query.CountAsync();

                // AcceptanceCriteria is a jsonb column with a value converter (not a real
                // EF relation), so it can't be projected via ProjectTo/SQL translation.
                // Materialize the entities first, then map in-memory.
                var entities = await query.OrderByDescending(us => us.CreatedAt).ToListAsync();
                var items = _mapper.Map<List<UserStoryDto>>(entities);
                var result = new PagedResult<UserStoryDto>
                {
                    TotalCount = totalCount,
                    Items = items,
                    PageNumber=1,
                    PageSize=totalCount

                };

                return items.Any()
                    ? Response<PagedResult<UserStoryDto>>.Success(result, "User stories fetched successfully", 200)
                    : Response<PagedResult<UserStoryDto>>.Success(new PagedResult<UserStoryDto>(), "No user stories found", 204);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user stories for project {ProjectId}", projectId);

                return Response<PagedResult<UserStoryDto>>.Failure(new PagedResult<UserStoryDto>(), "An unexpected error occurred while retrieving user stories",500,new List<string> { ex.Message });
            }
        }
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

    }
}
