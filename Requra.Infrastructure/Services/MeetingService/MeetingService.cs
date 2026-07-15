using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Meeting;
using Requra.Application.DTOs.Project.ProjectCreation;
using Requra.Application.DTOs.ProjectMembers;
using Requra.Application.Interfaces.IMeetingService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;

namespace Requra.Infrastructure.Services.MeetingService
{
    public class MeetingService(RequraDbContext _context, IValidator<CreateMeetingRequest> _validator, ILogger<MeetingService> _logger, IMapper _mapper) : IMeetingService
    {

        public async Task<Response<MeetingDto>> CreateMeetingAsync(
            Guid projectId,
            CreateMeetingRequest request,
            string currentUserId)
        {
            try
            {
                //will be authomated later also after handeling global response 
                var validation = await _validator.ValidateAsync(request);

                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                    return Response<MeetingDto>.Failure(null, "Validation failed", 422, errors);
                }
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                    return Response<MeetingDto>.Failure(null, "Project not found", 404);

                var isMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId);
                if (!isMember)
                    return Response<MeetingDto>.Failure(null, "You are not allowed to access this project", 403);

                var meeting = new MeetingSession(
                    projectId,
                    hostId: currentUserId,
                    createdById: currentUserId,
                    scheduledAt: request.ScheduledAt,
                    title: request.Title,
                    description: request.Description
                );
                //will be edited after know more about join url

                var joinUrl = $"https://app.requra.ai/meetings/{meeting.Id}/join";
                meeting.SetPlatform(joinUrl);

                var hostParticipant = new MeetingParticipant(
                    currentUserId,
                    meeting.Id,
                    MeetingRole.Host
                );

                _context.MeetingSessions.Add(meeting);
                _context.MeetingParticipants.Add(hostParticipant);

                await _context.SaveChangesAsync();
                //use auto mapper later to map the meeting entity to meeting dto
                var dto = new MeetingDto
                {
                    Id = meeting.Id,
                    ProjectId = meeting.ProjectId,
                    Title = meeting.Title,
                    Description = meeting.Description,
                    Status = meeting.Status.ToString().ToUpper(),
                    JoinUrl = joinUrl,
                    CreatedById = meeting.CreatedById,
                    HostParticipantId = hostParticipant.UserId,
                    ScheduledAt = meeting.ScheduledAt,
                    StartedAt = meeting.StartedAt,
                    EndedAt = meeting.EndedAt,
                    CreatedAt = meeting.CreatedAt,
                    UpdatedAt = meeting.UpdatedAt
                };

                return Response<MeetingDto>.Success(
                    dto,
                    "Meeting created successfully",
                    201);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the meeting");
                return Response<MeetingDto>.Failure(
                    null,
                    "An error occurred while creating the meeting",
                    500,
                    new List<string>() { ex.Message });
            }
        }
        public async Task<Response<PagedResult<ProjectMeetingsDto>>> GetMeetingsAsync(
    Guid projectId, string currentUserId,
    GetMeetingsQuery query)
        {
            try
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                    return Response<PagedResult<ProjectMeetingsDto>>.Failure(null, "Project not found", 404);

                var isMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId);
                if (!isMember)
                    return Response<PagedResult<ProjectMeetingsDto>>.Failure(null, "You are not allowed to access this project", 403);
                var baseQuery = _context.MeetingSessions
                .Where(m => m.ProjectId == projectId);

                if (query.Status.HasValue)
                {
                    baseQuery = baseQuery.Where(m => m.Status == query.Status.Value);
                }

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var search = query.Search.ToLower();

                    baseQuery = baseQuery.Where(m =>
                        m.Title!.ToLower().Contains(search) ||
                        m.Description!.ToLower().Contains(search));
                }

                var totalCount = await baseQuery.CountAsync();

                var items = await baseQuery
    .OrderByDescending(m => m.CreatedAt)
    .Skip((query.PageNumber - 1) * query.PageSize)
    .Take(query.PageSize)
    .Select(m => new ProjectMeetingsDto
    {
        Id = m.Id,
        ProjectId = m.ProjectId,
        Title = m.Title,
        Description = m.Description,
        Status = m.Status.ToString().ToUpper(),
        JoinUrl = $"https://app.requra.ai/meetings/{m.Id}/join",
        CreatedById = m.CreatedById,
        ScheduledAt = m.ScheduledAt,
        StartedAt = m.StartedAt,
        EndedAt = m.EndedAt,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt,

        ParticipantsCount = m.Participants.Count,

        HostParticipantId = m.Participants
            .Where(p => p.Role == MeetingRole.Host)
            .Select(p => p.UserId)
            .FirstOrDefault(),

        ActiveRecordingId = m.Recordings
            //.Where(r => r.IsActive) //check later if we need to filter by active recordings
            .Select(r => (Guid?)r.Id)
            .FirstOrDefault()
    })
    .ToListAsync();

                var result = new PagedResult<ProjectMeetingsDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };

                return Response<PagedResult<ProjectMeetingsDto>>.Success(
                    result,
                    totalCount>0 ? "Meetings retrieved successfully" : "No meetings found",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving meetings");
                return Response<PagedResult<ProjectMeetingsDto>>.Failure(
                    null,
                    "An error occurred while retrieving meetings",
                    500,
                    new List<string>() { ex.Message });
            }
        }
        public async Task<Response<MeetingDetailsDto>> GetMeetingByIdAsync(
    Guid meetingId,
    string currentUserId)
        {
            try
            {

                var meeting = await _context.MeetingSessions
                 .Where(m => m.Id == meetingId)
                 .Select(m => new
                 {
                     m.Id,
                     m.ProjectId,
                     m.Title,
                     m.Description,
                     m.Status,
                     m.CreatedById,
                     m.ScheduledAt,
                     m.StartedAt,
                     m.EndedAt,
                     m.CreatedAt,
                     m.UpdatedAt,

                     ParticipantsCount = m.Participants.Count,

                     HostParticipantId = m.Participants
                         .Where(p => p.Role == MeetingRole.Host)
                         .Select(p => p.UserId)
                         .FirstOrDefault(),

                     ActiveRecordingId = m.Recordings
                         //.Where(r => r.IsActive) //check later if we need to filter by active recordings
                         .Select(r => (Guid?)r.Id)
                         .FirstOrDefault(),

                     CurrentUserParticipant = m.Participants
                         .Where(p => p.UserId == currentUserId)
                         .Select(p => new
                         {
                             p.UserId,
                             p.Role
                         })
                         .FirstOrDefault()
                 })
                 .FirstOrDefaultAsync();

                if (meeting == null)
                    return Response<MeetingDetailsDto>.Failure("Meeting not found", 404);

                if (meeting.CurrentUserParticipant == null)
                    return Response<MeetingDetailsDto>.Failure("Forbidden", 403);

                var isHost = meeting.CurrentUserParticipant.Role == MeetingRole.Host;

                var canStart = isHost && meeting.Status == MeetingStatus.Scheduled;
                var canEnd = isHost && meeting.Status == MeetingStatus.Live;
                var canInvite = isHost;
                var canRecord = isHost && meeting.Status == MeetingStatus.Live;

                var dto = new MeetingDetailsDto
                {
                    Id = meeting.Id,
                    ProjectId = meeting.ProjectId,
                    Title = meeting.Title,
                    Description = meeting.Description,
                    Status = meeting.Status.ToString().ToUpper(),
                    JoinUrl = $"https://app.requra.ai/meetings/{meeting.Id}/join",
                    CreatedById = meeting.CreatedById,
                    HostParticipantId = meeting.CreatedById, //until separate host and creator

                    ScheduledAt = meeting.ScheduledAt,
                    StartedAt = meeting.StartedAt,
                    EndedAt = meeting.EndedAt,
                    CreatedAt = meeting.CreatedAt,
                    UpdatedAt = meeting.UpdatedAt,

                    ParticipantsCount = meeting.ParticipantsCount,
                    ActiveRecordingId = meeting.ActiveRecordingId,

                    CurrentUserRole = isHost ? "HOST" : "PARTICIPANT", //later will add viewer role if needed
                    CanStart = canStart,
                    CanEnd = canEnd,
                    CanInvite = canInvite,
                    CanRecord = canRecord
                };

                return Response<MeetingDetailsDto>.Success(
                    dto,
                    "Meeting details retrieved successfully",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving meeting details");
                return Response<MeetingDetailsDto>.Failure(
                    "An error occurred while retrieving meeting details",
                    500,
                    new List<string>() { ex.Message });
            }
        }

        public async Task<Response<MeetingDto>> CancelMeetingAsync(
    Guid meetingId,
    string currentUserId)
        {
            try
            {
                var meeting = await _context.MeetingSessions
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

                if (meeting == null)
                    return Response<MeetingDto>.Failure("Meeting not found", 404);

                var currentUserParticipant = meeting.Participants
                    .FirstOrDefault(p => p.UserId == currentUserId);

                //  Must be participant
                if (currentUserParticipant == null)
                    return Response<MeetingDto>.Failure("Forbidden", 403);

                // Must be host
                if (currentUserParticipant.Role != MeetingRole.Host)
                    return Response<MeetingDto>.Failure("Only host can cancel meeting", 403);

                if (meeting.Status == MeetingStatus.Cancelled)
                    return Response<MeetingDto>.Failure("Meeting already cancelled", 400);

                if (meeting.Status == MeetingStatus.Ended)
                    return Response<MeetingDto>.Failure("Cannot cancel ended meeting", 400);

                meeting.Cancel();

                await _context.SaveChangesAsync();



                var dto = new MeetingDto
                {
                    Id = meeting.Id,
                    ProjectId = meeting.ProjectId,
                    Title = meeting.Title,
                    Description = meeting.Description,
                    Status = meeting.Status.ToString().ToUpper(),
                    JoinUrl = $"https://app.requra.ai/meetings/{meeting.Id}/join",
                    CreatedById = meeting.CreatedById,
                    HostParticipantId = currentUserParticipant.UserId,

                    ScheduledAt = meeting.ScheduledAt,
                    StartedAt = meeting.StartedAt,
                    EndedAt = meeting.EndedAt,
                    CreatedAt = meeting.CreatedAt,
                    UpdatedAt = meeting.UpdatedAt
                };

                return Response<MeetingDto>.Success(
                    dto,
                    "Meeting cancelled successfully",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cancelling the meeting");
                return Response<MeetingDto>.Failure(
                    "An error occurred while cancelling the meeting",
                    500,
                    new List<string>() { ex.Message });
            }
        }

        public async Task<Response<MeetingDto>> UpdateMeetingAsync(
    Guid meetingId,
    UpdateMeetingRequest request,
    string currentUserId)
        {
            var meeting = await _context.MeetingSessions
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null)
                return Response<MeetingDto>.Failure("Meeting not found", 404);

            var currentUserParticipant = meeting.Participants
                .FirstOrDefault(p => p.UserId == currentUserId);

            if (currentUserParticipant == null)
                return Response<MeetingDto>.Failure("Forbidden", 403);

            if (currentUserParticipant.Role != MeetingRole.Host)
                return Response<MeetingDto>.Failure("Only host can update meeting", 403);

            if (meeting.Status == MeetingStatus.Ended)
                return Response<MeetingDto>.Failure("Cannot update ended meeting", 400);

            if (meeting.Status == MeetingStatus.Cancelled)
                return Response<MeetingDto>.Failure("Cannot update cancelled meeting", 400);

            meeting.UpdateDetails(request.Title,request.Description,request.ScheduledAt);

            await _context.SaveChangesAsync();


            var dto = new MeetingDto
            {
                Id = meeting.Id,
                ProjectId = meeting.ProjectId,
                Title = meeting.Title,
                Description = meeting.Description,
                Status = meeting.Status.ToString().ToUpper(),
                JoinUrl = $"https://app.requra.ai/meetings/{meeting.Id}/join",
                CreatedById = meeting.CreatedById,
                HostParticipantId = currentUserParticipant.UserId,

                ScheduledAt = meeting.ScheduledAt,
                StartedAt = meeting.StartedAt,
                EndedAt = meeting.EndedAt,
                CreatedAt = meeting.CreatedAt,
                UpdatedAt = meeting.UpdatedAt,

            };

            return Response<MeetingDto>.Success(
                dto,
                "Meeting updated successfully",
                200);
        }
    }
}
