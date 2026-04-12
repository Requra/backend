using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Project.ProjectResults.UserStory;
using Requra.Application.Interfaces.IProjectService.IProjectResultsService.IUserStoryService;
using Requra.Application.Response;
using Requra.Domain.Entities;
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

                var query = _context.UserStories.AsNoTracking().Where(us => us.ProjectId == projectId);
                var totalCount = await query.CountAsync();
                var items = await query.OrderByDescending(us => us.CreatedAt).ProjectTo<UserStoryDto>(_mapper.ConfigurationProvider).ToListAsync();

                var result = new PagedResult<UserStoryDto>
                {
                    TotalCount = totalCount,
                    Items = items
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
    }
}
