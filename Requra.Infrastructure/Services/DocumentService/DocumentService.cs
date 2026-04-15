using AutoMapper;
using AutoMapper.QueryableExtensions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs.Document;
using Requra.Application.Interfaces.IDocumentService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;
using Requra.Infrastructure.Services.ProjectService.ProjectResultsService.UserStoryService;
using Requra.Infrastructure.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Services.DocumentService
{
    public class DocumentService(IUnitOfWork _unitOfWork, RequraDbContext _context, IMapper _mapper, ILogger<UserStoryService> _logger, ICloudinaryService _cloudinaryService) : IDocumentService
    {
        public async Task<Response<List<DocumentDto>>> GetDocumentsByProjectIdAsync(Guid projectId)
        {
            if (projectId == Guid.Empty)
                return Response<List<DocumentDto>>.Failure(new List<DocumentDto>(), "Invalid ProjectId", 400);

            try
            {
                var projectRepo = _unitOfWork.Repository<Project>();
                var projectExists = await projectRepo.GetByIdAsync(projectId);

                if (projectExists == null)
                    return Response<List<DocumentDto>>.Failure(new List<DocumentDto>(), "Project not found", 404);

                var documents = await _context.Documents
                    .AsNoTracking()
                    .Where(d => d.ProjectId == projectId)
                    .OrderByDescending(d => d.UpdatedAt).ProjectTo<DocumentDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                return documents.Any()
                    ? Response<List<DocumentDto>>.Success(documents, "Documents fetched successfully", 200)
                    : Response<List<DocumentDto>>.Success(new List<DocumentDto>(), "No documents found", 204);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for project {ProjectId}", projectId);

                return Response<List<DocumentDto>>.Failure(new List<DocumentDto>(), "An unexpected error occurred while retrieving documents", 500, new List<string> { ex.Message });
            }
        }

        public async Task<Response<DocumentDto>> UploadDocumentAsync(UploadDocumentDto model, string userId, CancellationToken cancellationToken = default)
        {
            if (model == null)
                return Response<DocumentDto>.Failure(new(), "Invalid model", 400);

            if (model.File == null || model.File.Length == 0)
                return Response<DocumentDto>.Failure(new(), "File is required", 400);

            if (string.IsNullOrEmpty(userId))
                return Response<DocumentDto>.Failure(new(), "Invalid user", 400);

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var projectRepo = _unitOfWork.Repository<Project>();
                var project = await projectRepo.GetByIdAsync(model.ProjectId);

                if (project == null)
                    return Response<DocumentDto>.Failure(new(), "Project not found", 404);

                var folder = $"projects/{model.ProjectId}/documents";

                var uploadResult = await _cloudinaryService.UploadFileAsync(model.File, folder, cancellationToken);

                var document = new Document(model.ProjectId, userId, model.Title, model.Type, model.Language);
                document.SetStorage(uploadResult.Url, uploadResult.Size);

                if (model.MeetingId.HasValue)
                {
                    //later added method → document.AttachToMeeting(model.MeetingId.Value);
                }


                await _unitOfWork.Repository<Document>().AddAsync(document);
                await _unitOfWork.SaveAsync();

                await transaction.CommitAsync(cancellationToken);

                var dto = _mapper.Map<DocumentDto>(document);

                return Response<DocumentDto>.Success(dto, "Document uploaded successfully", 201);
            }
            catch (OperationCanceledException)
            {
                await transaction.RollbackAsync();
                return Response<DocumentDto>.Failure(new(), "Operation cancelled", 499);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error uploading document");

                return Response<DocumentDto>.Failure(
                    new(),
                    "An error occurred while uploading document",
                    500,
                    new List<string> { ex.Message });
            }
        }
    }
}
