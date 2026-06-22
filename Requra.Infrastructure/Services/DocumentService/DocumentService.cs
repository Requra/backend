using AutoMapper;
using AutoMapper.QueryableExtensions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DocumentFormat.OpenXml.Packaging;
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
using DocumentFormat.OpenXml.Wordprocessing;
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

                var document = new Domain.Entities.Document(model.ProjectId, userId, model.Title, model.Type, model.Language);
                document.SetStorage(uploadResult.Url, uploadResult.Size);

                if (model.MeetingId.HasValue)
                {
                    //later added method → document.AttachToMeeting(model.MeetingId.Value);
                }


                await _unitOfWork.Repository<Domain.Entities.Document>().AddAsync(document);
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

        public async Task<string> GetCombinedText(Guid projectId, List<Guid> documentIds)
        {
            // 1. Fetch documents using UnitOfWork
            var documents =_context.Documents.Where(d =>
                d.ProjectId == projectId &&
                documentIds.Contains(d.Id)).ToList();
            //var documents = await _unitOfWork.Documents.GetAllAsync(d =>
            //    d.ProjectId == projectId &&
            //    documentIds.Contains(d.Id));

            if (documents == null || !documents.Any())
                throw new Exception("No documents found for this project.");

            documents = documents.OrderBy(d => d.CreatedAt).ToList();

            var combinedText = new StringBuilder();

            // 2. Extract + Combine
            foreach (var doc in documents)
            {
                var extractedText = await ExtractTextAsync(doc);

                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    combinedText.AppendLine($"--- Document: {doc.Title} ---");
                    combinedText.AppendLine(extractedText);
                    combinedText.AppendLine();
                }
            }

            var result = combinedText.ToString();

            if (result.Length > 20000)
                result = result.Substring(0, 20000);

            return result;
        }

        private async Task<string> ExtractTextAsync(Domain.Entities.Document document)
        {
            if (!string.IsNullOrWhiteSpace(document.TranscriptText))
                return document.TranscriptText;

            if (!string.IsNullOrWhiteSpace(document.StorageUrl))
            {
                if (!File.Exists(document.StorageUrl))
                    return string.Empty;

                var extension = Path.GetExtension(document.StorageUrl).ToLower();

                return extension switch
                {
                    ".txt" => await File.ReadAllTextAsync(document.StorageUrl),

                    ".docx" => ExtractDocxText(document.StorageUrl),

                    ".pdf" => "[PDF extraction not implemented yet]",

                    _ => "[Unsupported file type]"
                };
            }

            return string.Empty;
        }
        private string ExtractDocxText(string filePath)
        {
            try
            {
                using var wordDoc = WordprocessingDocument.Open(filePath, false);

                var body = wordDoc.MainDocumentPart?.Document?.Body;

                if (body == null)
                    return string.Empty;

                var paragraphs = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();

                var text = new StringBuilder();

                foreach (var para in paragraphs)
                {
                    text.AppendLine(para.InnerText);
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                return $"[DOCX Extraction Error: {ex.Message}]";
            }
        }
    }
}
