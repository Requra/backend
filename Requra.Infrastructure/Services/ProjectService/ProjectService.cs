using AutoMapper;
using AutoMapper.Execution;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.DTOs.Project;
using Requra.Application.DTOs.Project.ProjectCreation;
using Requra.Application.DTOs.Project.ProjectDetails;
using Requra.Application.DTOs.Project.ProjectUpdate;
using Requra.Application.Interfaces.IProjectRepository;
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
    public class ProjectService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProjectService> logger, RequraDbContext context, UserManager<ApplicationUser> userManager, IProjectRepository projectRepository, IValidator<ProjectRequestDto> validator,IValidator<ProjectUpdateRequestDto> updateValidator) : IProjectService
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

        public async Task<Response<ProjectResponseDto>> CreateProjectAsync(ProjectRequestDto request, string currentUserId)
        {
            try
            { //Validation Handeled Here only For Now due To Problem In Auto Validation, Will be Solved Later "All validation errors In Response"

                var validation = await validator.ValidateAsync(request);

                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                    return Response<ProjectResponseDto>.Failure(new ProjectResponseDto(), "Validation failed", 400, errors);
                }
                var client = await userManager.FindByEmailAsync(request.ClientEmail);

                if (client == null)
                {
                    return Response<ProjectResponseDto>.Failure(new ProjectResponseDto(),
                        "Client does not exist",
                        404
                    );
                }
                //May Need Refactoring Later

                var project = new Project(request.Name, request.Description, request.ProjectType);

                project.AddMember(currentUserId, ProjectRole.Owner);

                project.AddMember(client.Id, ProjectRole.Viewer);

                foreach (var member in request.TeamMembers)
                {
                    var user = await userManager.FindByEmailAsync(member.Email);

                    if (user == null){
                        return Response<ProjectResponseDto>.Failure(new (),
                        $"{member.Email} does not exist",
                        404
                    );
                    }

                    if (project.Members.Any(m => m.UserId == user.Id))
                        continue;

                    // Send Invitation Email After Accepting Invitation, the user will be added as a contributor

                    project.Members.Add(new ProjectMember(
                        user.Id,
                        project.Id,
                        ProjectRole.Contributor
                    ));

                }

                await projectRepository.AddAsync(project);

                var response = new ProjectResponseDto
                {
                    Id = project.Id.ToString(),
                    Name = project.Name,
                    Description = project.Description,
                    ProjectType = project.ProjectType,
                    Status = project.Status.ToString(),
                    ClientEmail = client.Email!,
                    CreatedAt = project.CreatedAt
                };

                return Response<ProjectResponseDto>.Success(
                    response,
                    "Project Created Successfully",
                    201
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project for user {UserId}", currentUserId);
                return Response<ProjectResponseDto>.Failure(new ProjectResponseDto(), "An unexpected error occurred while creating the project", 500, new List<string> { ex.Message });
            }
        }

        public async Task<Response<ProjectDetailsDto>> GetProjectByIdAsync(Guid projectId, string currentUserId)
        {
            try
            {
                var project = await projectRepository.GetByIdWithMembersAsync(projectId);

                if (project == null || project.IsDeleted)
                {
                    return Response<ProjectDetailsDto>.Failure(
                        new ProjectDetailsDto(),
                        "Project not found",
                        404
                    );
                }

                var isMember = project.Members.Any(m => m.UserId == currentUserId);

                if (!isMember)
                {
                    return Response<ProjectDetailsDto>.Failure(new ProjectDetailsDto(),
                        "You are not allowed to access this project",
                        403
                    );
                }

                var clientMember = project.Members.FirstOrDefault(m => m.Role == ProjectRole.Viewer);

                var clientUser = clientMember != null
                    ? await userManager.FindByIdAsync(clientMember.UserId)
                    : null;

                var dto = new ProjectDetailsDto
                {
                    Id = project.Id.ToString(),
                    Name = project.Name,
                    Description = project.Description,
                    ProjectType = project.ProjectType.ToString(),
                    Status = project.Status.ToString(),
                    ClientEmail = clientUser?.Email ?? "Unknown",
                    CreatedAt = project.CreatedAt,

                    TeamMembers = project.Members
                        .Where(m => m.User != null && m.User.Email != null)
                        .Select(m => new TeamMemberDto
                        {
                            Email = m.User.Email!
                        })
                        .ToList()
                };

                return Response<ProjectDetailsDto>.Success(
                    dto,
                    "Project Fetched Successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project with id {ProjectId}", projectId);
                return Response<ProjectDetailsDto>.Failure(new ProjectDetailsDto(), "An unexpected error occurred while retrieving the project", 500, new List<string> { ex.Message });
            }
        }

        public async Task<Response<bool>> DeleteProjectAsync(Guid id, string currentUserId)
        {

            try
            {
                var project = await projectRepository.GetByIdWithMembersAsync(id);
                if (project == null || project.IsDeleted)
                {
                    return Response<bool>.Failure(false, "Project not found", 404);
                }
                var isOwner = project.Members.Any(m => m.UserId == currentUserId && m.Role == ProjectRole.Owner);
                if (!isOwner)
                {
                    return Response<bool>.Failure(false, "Unauthorized", 403);
                }

                project.Delete();

                await projectRepository.SaveChangesAsync();

                return Response<bool>.Success(
                    true,
                    "Project Deleted Successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project with id {ProjectId}", id);
                return Response<bool>.Failure(false, "An unexpected error occurred while deleting the project", 500, new List<string> { ex.Message });
            }

        }

        

        public async Task<Response<ProjectUpdateResponseDto>> UpdateProjectAsync(Guid id, ProjectUpdateRequestDto dto, string currentUserId)
        {
            try
            { //Validation Handeled Here only For Now due To Problem In Auto Validation, Will be Solved Later "All validation errors In Response"

                var validation = await updateValidator.ValidateAsync(dto);

                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                    return Response<ProjectUpdateResponseDto>.Failure(new (), "Validation failed", 400, errors);
                }

                var project = await projectRepository.GetByIdWithMembersAsync(id);
                if (project == null || project.IsDeleted)
                {
                    return Response<ProjectUpdateResponseDto>.Failure(new(),"Project not found", 404);
                }
                var isOwner = project.Members.Any(m => m.UserId == currentUserId && m.Role == ProjectRole.Owner);
                if (!isOwner)
                {
                    return Response<ProjectUpdateResponseDto>.Failure(new(), "Only the owner can update the project", 403);
                }
            

                project.UpdateDetails(dto.Name, dto.Description, dto.ProjectType, dto.Status, dto.Language);



                if (dto.TeamMembers != null && dto.TeamMembers.Any())
                {
                    var emails = dto.TeamMembers
                        .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                        .Select(x => x.Email.Trim().ToLower())
                        .ToList();

                    var users = await userManager.Users
                            .Where(u => u.Email != null && emails.Contains(u.Email.ToLower()))
                            .ToListAsync();

                    var userDict = users
                        .Where(u => !string.IsNullOrWhiteSpace(u.Email))
                        .ToDictionary(
                              u => u.Email!.Trim().ToLower(),
                              u => u
                         );
                    var missingEmails = emails
                     .Where(email => !userDict.ContainsKey(email))
                     .ToList();

                    if (missingEmails.Any())
                    {
                        return Response<ProjectUpdateResponseDto>.Failure(
                            new(),
                            $"These emails do not exist: {string.Join(", ", missingEmails)}",
                            404
                        );
                    }

                    var incomingUserIds = dto.TeamMembers
                        .Select(x => x.Email.Trim().ToLower())
                        .Where(userDict.ContainsKey)
                        .Select(email => userDict[email].Id)
                        .ToHashSet();

                    var toRemove = project.Members
                        .Where(m => !incomingUserIds.Contains(m.UserId))
                        .ToList();

                    foreach (var member in toRemove)
                    {
                        project.Members.Remove(member);
                    }

                    foreach (var user in users)
                    {

                        if (project.Members.Any(m => m.UserId == user.Id))
                            continue;

                        // Send Invitation Email After Accepting Invitation, the user will be added as a contributor

                        project.Members.Add(new ProjectMember(
                            user.Id,
                            project.Id,
                            ProjectRole.Contributor
                        ));
                    }
                }
                if (dto.ClientEmail != null)
                {
                    var client = await userManager.FindByEmailAsync(dto.ClientEmail);
                    if (client == null)
                    {
                        return Response<ProjectUpdateResponseDto>.Failure(new(),
                            "Client does not exist",
                            404
                        );
                    }
                    project.AddMember(client.Id, ProjectRole.Viewer);
                }

                await projectRepository.SaveChangesAsync();

                var response = new ProjectUpdateResponseDto
                {
                    Id = project.Id.ToString(),
                    Name = project.Name,
                    Description = project.Description,
                    ProjectType = project.ProjectType,
                    Status = project.Status,
                    ClientEmail = project.Members.FirstOrDefault(m => m.Role == ProjectRole.Viewer)?.User.Email ?? "Unknown",
                    TeamMembers = project.Members
                        .Where(m => m.User != null && m.User.Email != null)
                        .Select(m => new TeamMemberDto
                        {
                            Email = m.User.Email!
                        })
                        .ToList(),
                    CreatedAt = project.CreatedAt,
                    UpdatedAt = project.UpdatedAt
                };

                return Response<ProjectUpdateResponseDto>.Success(
                    response,
                    "Project Updated Successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project for user {UserId}", currentUserId);
                return Response<ProjectUpdateResponseDto>.Failure(new (), "An unexpected error occurred while updating the project", 500, new List<string> { ex.Message });
            }
        }

    }
}




