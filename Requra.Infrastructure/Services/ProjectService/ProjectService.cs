using AutoMapper;
using AutoMapper.Execution;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Project;
using Requra.Application.DTOs.Project.ProjectCreation;
using Requra.Application.Interfaces.IProjectService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Domain.Specifications.ProjectsSpecification;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.Specification;
using Requra.Infrastructure.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Requra.Infrastructure.Services.ProjectService
{
    public class ProjectService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProjectService> logger, RequraDbContext context) : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ILogger<ProjectService> _logger = logger;
        private readonly IMapper _mapper = mapper;
        private readonly RequraDbContext _context = context;


        public async Task<Response<PagedResult<ProjectDTO>>> GetUserProjectsAsync(ProjectFilter filter)
        {

            if (string.IsNullOrEmpty(filter.UserId))
            {
                return Response<PagedResult<ProjectDTO>>.Failure(new PagedResult<ProjectDTO>(), "Invalid CreatorId", 400);
            }

            try
            {
                var isUserExist = await _unitOfWork.Users.GetByIdAsync(filter.UserId);
                if (isUserExist == null)
                {
                    return Response<PagedResult<ProjectDTO>>.Failure(new PagedResult<ProjectDTO>(), "User Not Found", 404);
                }
                var repo = _unitOfWork.Repository<Project>();

                // Apply specification for projects
                var spec = new ProjectsByUserSpecification(filter.UserId, filter.Status, filter.PageNumber, filter.PageSize);
                var countSpec = new ProjectsCountSpecification(filter.UserId, filter.Status);

                // var projects = await repo.ListAsync(spec);
                var totalCount = await repo.CountAsync(countSpec);

                var query = _context.Projects.AsQueryable();

                query = SpecificationEvaluator<Project>.GetQuery(query, spec);

                var mapped = await query
                    .ProjectTo<ProjectDTO>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                return mapped.Any() ? Response<PagedResult<ProjectDTO>>.Success(new PagedResult<ProjectDTO> { Items = mapped, TotalCount = totalCount, PageNumber = filter.PageNumber, PageSize = filter.PageSize }, "Projects Fetched Successfully", 200)
                : Response<PagedResult<ProjectDTO>>.Success(new PagedResult<ProjectDTO>(), "No projects found", 204);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects for user {UserId}", filter.UserId);

                return Response<PagedResult<ProjectDTO>>.Failure("An unexpected error occurred while retrieving projects", 500, new List<string> { ex.Message });
            }
        }
    }
}

        


        