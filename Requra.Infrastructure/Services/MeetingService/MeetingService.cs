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
    public class MeetingService(RequraDbContext _context,IValidator<CreateMeetingRequest> _validator, ILogger<MeetingService> _logger) : IMeetingService
    {

        public async Task<Response<MeetingDto>> CreateMeetingAsync(
            Guid projectId,
            CreateMeetingRequest request,
            string currentUserId)
        {
            try
            {
                //will be authomated later also after handeling global response and exception handling
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
    }
}
