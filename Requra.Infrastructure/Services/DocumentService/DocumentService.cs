using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs.Document;
using Requra.Application.Interfaces.IDocumentService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.Services.ProjectService.ProjectResultsService.UserStoryService;
using Requra.Infrastructure.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Services.DocumentService
{
    public class DocumentService(IUnitOfWork _unitOfWork, RequraDbContext _context, IMapper _mapper, ILogger<UserStoryService> _logger) : IDocumentService
    {
        public async Task<Response<List<DocumentDto>>> GetDocumentsByProjectIdAsync(Guid projectId)
        {
            if (projectId == Guid.Empty)
                return Response<List<DocumentDto>>.Failure(new List<DocumentDto>(),"Invalid ProjectId",400);

            try
            {
                var projectRepo = _unitOfWork.Repository<Project>();
                var projectExists = await projectRepo.GetByIdAsync(projectId);

                if (projectExists == null)
                    return Response<List<DocumentDto>>.Failure(new List<DocumentDto>(),"Project not found",404);

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

                return Response<List<DocumentDto>>.Failure(new List<DocumentDto>(),"An unexpected error occurred while retrieving documents",500,new List<string> { ex.Message });
            }
        }
    }
}
