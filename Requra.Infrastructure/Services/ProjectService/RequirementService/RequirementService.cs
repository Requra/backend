using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Requra.Application.DTOs.Project.Requirements;
using Requra.Application.Interfaces.IProjectService.IRequirementService;
using Requra.Application.Response;
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
                        .Select(x => new RequirementSourceRefDto
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
                    Quality = new RequirementQualityDto
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


    }
}
