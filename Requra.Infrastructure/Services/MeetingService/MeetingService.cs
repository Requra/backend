using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Invitation.MeetingInvitation;
using Requra.Application.DTOs.Meeting;
using Requra.Application.DTOs.Project.ProjectCreation;
using Requra.Application.DTOs.ProjectMembers;
using Requra.Application.DTOs.Recordings;
using Requra.Application.Interfaces.IMeetingService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.IEmailSender;
using Requra.Infrastructure.ExternalServices.EmailSender;
using Requra.Infrastructure.Services.InvitationService.MeetingInvitationService;
using System.Security.Claims;

namespace Requra.Infrastructure.Services.MeetingService
{
    public class MeetingService(RequraDbContext _context, IValidator<CreateMeetingRequest> _validator, ILogger<MeetingService> _logger, IMapper _mapper, IEmailSender _emailSender) : IMeetingService
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






        public async Task<Response<StartMeetingResponse>> StartMeetingAsync(Guid MeetingId , CancellationToken cancellationToken = default)
        {
            if (MeetingId == Guid.Empty)
            {
                return Response<StartMeetingResponse>.Failure(new StartMeetingResponse(),"MeetingId is required.",StatusCodes.Status400BadRequest);
            }

            try
            {
                var meeting = await _context.MeetingSessions.FirstOrDefaultAsync(x => x.Id == MeetingId, cancellationToken);

                if (meeting is null)
                {
                    return Response<StartMeetingResponse>.Failure(new StartMeetingResponse(),"Meeting not found.",StatusCodes.Status404NotFound);
                }

                var previousStatus = meeting.Status.ToString().ToUpper();

                if (meeting.Status != MeetingStatus.Scheduled)
                {
                    return Response<StartMeetingResponse>.Failure(new StartMeetingResponse(),$"Meeting cannot be started. Current status: {previousStatus}.",StatusCodes.Status409Conflict);
                }

                meeting.Start();
                await _context.SaveChangesAsync(cancellationToken);
                var response = new StartMeetingResponse
                {
                    MeetingId = meeting.Id,
                    PreviousStatus = previousStatus,
                    Status = meeting.Status.ToString().ToUpper(),
                    StartedAt = meeting.StartedAt,
                    EndedAt = meeting.EndedAt
                };

                return Response<StartMeetingResponse>.Success(response,"Meeting started successfully.",StatusCodes.Status200OK);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<StartMeetingResponse>.Failure(new StartMeetingResponse(),"A concurrency error occurred while starting the meeting.",StatusCodes.Status409Conflict,new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<StartMeetingResponse>.Failure(new StartMeetingResponse(),"A database error occurred while starting the meeting.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                return Response<StartMeetingResponse>.Failure(new StartMeetingResponse(),"An unexpected error occurred while starting the meeting.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
        }

        public async Task<Response<EndMeetingResponse>> EndMeetingAsync(Guid MeetingId,CancellationToken cancellationToken = default)
        {
            if (MeetingId == Guid.Empty)
            {
                return Response<EndMeetingResponse>.Failure(new EndMeetingResponse(),"MeetingId is required.",StatusCodes.Status400BadRequest);
            }

            try
            {
                var meeting = await _context.MeetingSessions.FirstOrDefaultAsync(x => x.Id == MeetingId, cancellationToken);

                if (meeting is null)
                {
                    return Response<EndMeetingResponse>.Failure(new EndMeetingResponse(),"Meeting not found.",StatusCodes.Status404NotFound);
                }

                var previousStatus = meeting.Status.ToString().ToUpper();

                if (meeting.Status == MeetingStatus.Ended)
                {
                    return Response<EndMeetingResponse>.Failure(new EndMeetingResponse(),"Meeting is already ended.",StatusCodes.Status409Conflict);
                }
                meeting.End();
                await _context.SaveChangesAsync(cancellationToken);

                var response = new EndMeetingResponse
                {
                    MeetingId = meeting.Id,
                    PreviousStatus = previousStatus,
                    Status = meeting.Status.ToString().ToUpper(),
                    StartedAt = meeting.StartedAt,
                    EndedAt = meeting.EndedAt
                };

                return Response<EndMeetingResponse>.Success(response,"Meeting ended successfully",StatusCodes.Status200OK);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<EndMeetingResponse>.Failure(new EndMeetingResponse(),"A concurrency error occurred while ending the meeting.",StatusCodes.Status409Conflict,new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<EndMeetingResponse>.Failure(new EndMeetingResponse(),"A database error occurred while ending the meeting.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                return Response<EndMeetingResponse>.Failure(new EndMeetingResponse(),"An unexpected error occurred while ending the meeting.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
        }

        public async Task<Response<InviteMeetingParticipantsResponse>> InviteParticipantsAsync(InviteMeetingParticipantsRequest request,CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request.MeetingId == Guid.Empty)
                errors.Add("MeetingId is required.");

            if (request.Members == null || !request.Members.Any())
                errors.Add("At least one member is required.");

            if (request.Members != null)
            {
                foreach (var member in request.Members)
                {
                    if (string.IsNullOrWhiteSpace(member.MemberId))
                        errors.Add("MemberId is required for each member item.");
                }
            }

            if (errors.Any())
            {
                return Response<InviteMeetingParticipantsResponse>.Failure(new InviteMeetingParticipantsResponse(),"Validation failed.",StatusCodes.Status400BadRequest,errors);
            }

            if (string.IsNullOrWhiteSpace(request.InvitedById))
            {
                return Response<InviteMeetingParticipantsResponse>.Failure(new InviteMeetingParticipantsResponse(),"Current user is not authenticated.",StatusCodes.Status401Unauthorized);
            }

            try
            {
                var meeting = await _context.MeetingSessions.FirstOrDefaultAsync(x => x.Id == request.MeetingId, cancellationToken);

                if (meeting is null)
                {
                    return Response<InviteMeetingParticipantsResponse>.Failure(new InviteMeetingParticipantsResponse(),"Meeting not found.",StatusCodes.Status404NotFound);
                }

                var requestedMemberIds = request.Members
                    .Where(x => !string.IsNullOrWhiteSpace(x.MemberId))
                    .Select(x => x.MemberId)
                    .Distinct()
                    .ToList();

                var projectMembers = await _context.ProjectMembers
                    .Include(x => x.User)
                    .Where(x => x.ProjectId == meeting.ProjectId && requestedMemberIds.Contains(x.UserId))
                    .ToListAsync(cancellationToken);

                var projectMemberMap = projectMembers.ToDictionary(x => x.UserId, x => x);

                var existingParticipants = await _context.MeetingParticipants
                    .Where(x => x.MeetingId == meeting.Id && requestedMemberIds.Contains(x.UserId))
                    .Select(x => x.UserId)
                    .ToListAsync(cancellationToken);

                var existingParticipantSet = existingParticipants.ToHashSet();
                var normalizedEmails = projectMembers
                    .Where(x => x.User != null && !string.IsNullOrWhiteSpace(x.User.Email))
                    .Select(x => x.User.Email!.Trim().ToLower())
                    .Distinct()
                    .ToList();

                var existingPendingInvitations = await _context.Invitations
                    .Where(x =>
                        x.MeetingId == meeting.Id &&
                        x.Status == InvitationStatus.Pending &&
                        normalizedEmails.Contains(x.Email.ToLower()))
                    .Select(x => x.Email.ToLower())
                    .ToListAsync(cancellationToken);

                var existingPendingInvitationSet = existingPendingInvitations.ToHashSet();


                var invitations = new List<Invitation>();
                var responseItems = new List<MeetingInvitationItemResponse>();

                foreach (var memberRequest in request.Members)
                {
                    if (!projectMemberMap.TryGetValue(memberRequest.MemberId, out var projectMember))
                    {
                        errors.Add($"Project member '{memberRequest.MemberId}' was not found in the meeting project.");
                        continue;
                    }

                    if (existingParticipantSet.Contains(memberRequest.MemberId))
                    {
                        errors.Add($"Member '{memberRequest.MemberId}' is already a participant in this meeting.");
                        continue;
                    }

                    var user = projectMember.User;
                    if (user == null)
                    {
                        errors.Add($"User '{memberRequest.MemberId}' could not be loaded.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(user.Email))
                    {
                        errors.Add($"User '{memberRequest.MemberId}' does not have a valid email.");
                        continue;
                    }

                    var normalizedEmail = user.Email.Trim().ToLower();

                    if (existingPendingInvitationSet.Contains(normalizedEmail))
                    {
                        errors.Add($"A pending invitation already exists for '{user.Email}'.");
                        continue;
                    }

                    var meetingRole = MeetingRole.Participant;

                    var stakeholderId = memberRequest.Role == ProjectRole.Viewer
                        ? memberRequest.MemberId
                        : null;
                    var projectMemberId = memberRequest.Role != ProjectRole.Viewer
                        ? memberRequest.MemberId
                        : null;

                    var displayName =
                        user.GetType().GetProperty("FullName")?.GetValue(user)?.ToString()
                        ?? user.UserName
                        ?? "User";

                    var invitation = new Invitation(
                        meetingId: meeting.Id,
                        inviteType: InviteType.Participant,
                        email: user.Email.Trim(),
                        displayName: displayName,
                        projectMemberId: projectMemberId,
                        stakeholderId: stakeholderId,
                        role: meetingRole,
                        invitedById: request.InvitedById,
                        expiresAt: DateTime.UtcNow.AddDays(3));

                    invitations.Add(invitation);
                    existingPendingInvitationSet.Add(normalizedEmail);
                }

                if (!invitations.Any() && errors.Any())
                {
                    return Response<InviteMeetingParticipantsResponse>.Failure(new InviteMeetingParticipantsResponse(),"No valid invitations could be created.",StatusCodes.Status400BadRequest,errors);
                }


                _context.Invitations.AddRange(invitations);
                await _context.SaveChangesAsync(cancellationToken);

                foreach (var invitation in invitations)
                {
                    responseItems.Add(new MeetingInvitationItemResponse
                    {
                        Id = invitation.Id,
                        MeetingId = request.MeetingId,
                        InviteType = InviteType.Participant,
                        Email = invitation.Email,
                        DisplayName = invitation.DisplayName,
                        ProjectMemberId = invitation.ProjectMemberId,
                        StakeholderId = invitation.StakeholderId,
                        Role = MeetingRole.Participant,
                        Status = invitation.Status,
                        InvitedById = invitation.InvitedById,
                        ExpiresAt = invitation.ExpiresAt,
                        CreatedAt = invitation.CreatedAt
                    });
                }
                var currentUser=_context.Users.FirstOrDefault(x => x.Id == request.InvitedById);
                foreach (var invitation in invitations)
                {
                    try
                    {
                        var subject = $"Invitation to meeting: {meeting.Title ?? "Meeting"}";

                        var body = MeetingInvitationTemplete.MeetingInvitationEmail(
                            userName: invitation.DisplayName ?? "User",
                            meetingTitle: meeting.Title ?? "Meeting",
                            inviteType: invitation.InviteType.ToString().ToUpper(),
                            meetingRole: invitation.Role.ToString().ToUpper(),
                            scheduledAt: meeting.ScheduledAt,
                            expiresAt: invitation.ExpiresAt,
                            meetingUrl: meeting.PlatformUrl,
                            invitedByName: currentUser.FullName ?? currentUser.UserName ?? "Requra Team",
                            meetingDescription: meeting.Description,
                            isGuest: false);

                        await _emailSender.SendEmailAsync(invitation.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send invitation email to {Email}", invitation.Email);
                    }
                }

                return Response<InviteMeetingParticipantsResponse>.Success(
                    new InviteMeetingParticipantsResponse
                    {
                        Items = responseItems
                    },
                    "Project members invited successfully",
                    StatusCodes.Status201Created);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<InviteMeetingParticipantsResponse>.Failure(new InviteMeetingParticipantsResponse(),"A concurrency error occurred while sending invitations.",StatusCodes.Status409Conflict,new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<InviteMeetingParticipantsResponse>.Failure(new InviteMeetingParticipantsResponse(),"A database error occurred while sending invitations.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                return Response<InviteMeetingParticipantsResponse>.Failure(new InviteMeetingParticipantsResponse(),"An unexpected error occurred while sending invitations.",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
        }

        public async Task<Response<InviteGuestsResponse>> InviteGuestsAsync(InviteGuestsRequest request,CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request.MeetingId == Guid.Empty)
                errors.Add("MeetingId is required.");

            if (request.Guests == null || !request.Guests.Any())
                errors.Add("At least one guest is required.");

            if (request.Guests != null)
            {
                foreach (var guest in request.Guests)
                {
                    if (string.IsNullOrWhiteSpace(guest.DisplayName))
                        errors.Add("Guest display name is required.");

                    if (string.IsNullOrWhiteSpace(guest.Email))
                        errors.Add("Guest email is required.");
                }
            }

            if (errors.Any())
            {
                return Response<InviteGuestsResponse>.Failure(new InviteGuestsResponse(),"Validation failed.",StatusCodes.Status400BadRequest,errors);
            }


            if (string.IsNullOrWhiteSpace(request.InvitedById))
            {
                return Response<InviteGuestsResponse>.Failure(new InviteGuestsResponse(),"Current user is not authenticated.",StatusCodes.Status401Unauthorized);
            }

            try
            {
                var meeting = await _context.MeetingSessions.FirstOrDefaultAsync(x => x.Id == request.MeetingId, cancellationToken);

                if (meeting is null)
                {
                    return Response<InviteGuestsResponse>.Failure(new InviteGuestsResponse(),"Meeting not found.",StatusCodes.Status404NotFound);
                }

                var currentUser = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.InvitedById, cancellationToken);

                if (currentUser is null)
                {
                    return Response<InviteGuestsResponse>.Failure(new InviteGuestsResponse(),"Current user not found.",StatusCodes.Status404NotFound);
                }

                var normalizedEmails = request.Guests
                    .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                    .Select(x => x.Email.Trim().ToLower())
                    .Distinct()
                    .ToList();

                var existingPendingGuestInvitations = await _context.Invitations
                    .Where(x =>
                        x.MeetingId == meeting.Id &&
                        x.InviteType == InviteType.Guest &&
                        x.Status == InvitationStatus.Pending &&
                        normalizedEmails.Contains(x.Email.ToLower()))
                    .Select(x => x.Email.ToLower())
                    .ToListAsync(cancellationToken);

                var existingPendingSet = existingPendingGuestInvitations.ToHashSet();

                var invitations = new List<Invitation>();
                var responseItems = new List<MeetingInvitationItemResponse>();

                foreach (var guest in request.Guests)
                {
                    var normalizedEmail = guest.Email.Trim().ToLower();

                    if (existingPendingSet.Contains(normalizedEmail))
                    {
                        errors.Add($"A pending guest invitation already exists for '{guest.Email}'.");
                        continue;
                    }

                    var invitation = new Invitation(
                        meetingId: meeting.Id,
                        inviteType: InviteType.Guest,
                        email: guest.Email.Trim(),
                        displayName: guest.DisplayName.Trim(),
                        projectMemberId: null,
                        stakeholderId: null,
                        role: MeetingRole.Viewer,
                        invitedById: request.InvitedById,
                        expiresAt: DateTime.UtcNow.AddDays(3));

                    invitations.Add(invitation);
                    existingPendingSet.Add(normalizedEmail);
                }

                if (!invitations.Any() && errors.Any())
                {
                    return Response<InviteGuestsResponse>.Failure(new InviteGuestsResponse(),"No valid guest invitations could be created.",StatusCodes.Status400BadRequest,errors);
                }

                _context.Invitations.AddRange(invitations);
                await _context.SaveChangesAsync(cancellationToken);

                foreach (var invitation in invitations)
                {
                    responseItems.Add(new MeetingInvitationItemResponse
                    {
                        Id = invitation.Id,
                        MeetingId = request.MeetingId,
                        InviteType = InviteType.Guest,
                        Email = invitation.Email,
                        DisplayName = invitation.DisplayName,
                        ProjectMemberId = invitation.ProjectMemberId,
                        StakeholderId = invitation.StakeholderId,
                        Role = MeetingRole.Viewer,
                        Status = invitation.Status,
                        InvitedById = invitation.InvitedById,
                        ExpiresAt = invitation.ExpiresAt,
                        CreatedAt = invitation.CreatedAt
                    });
                }

                foreach (var invitation in invitations)
                {
                    try
                    {
                        var subject = $"Guest invitation to meeting: {meeting.Title ?? "Meeting"}";

                        var body = MeetingInvitationTemplete.MeetingInvitationEmail(
                            userName: invitation.DisplayName ?? "Guest",
                            meetingTitle: meeting.Title ?? "Meeting",
                            inviteType: invitation.InviteType.ToString().ToUpper(),
                            meetingRole: invitation.Role.ToString().ToUpper(),
                            scheduledAt: meeting.ScheduledAt,
                            expiresAt: invitation.ExpiresAt,
                            meetingUrl: meeting.PlatformUrl,
                            invitedByName: currentUser.FullName ?? currentUser.UserName ?? "Requra Team",
                            meetingDescription: meeting.Description,
                            isGuest: true);

                        await _emailSender.SendEmailAsync(invitation.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send guest invitation email to {Email}", invitation.Email);
                    }
                }

                return Response<InviteGuestsResponse>.Success(
                    new InviteGuestsResponse
                    {
                        Items = responseItems
                    },
                    "Guests invited successfully",
                    StatusCodes.Status201Created);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<InviteGuestsResponse>.Failure(
                    new InviteGuestsResponse(),
                    "A concurrency error occurred while sending guest invitations.",
                    StatusCodes.Status409Conflict,
                    new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<InviteGuestsResponse>.Failure(
                    new InviteGuestsResponse(),
                    "A database error occurred while sending guest invitations.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                return Response<InviteGuestsResponse>.Failure(
                    new InviteGuestsResponse(),
                    "An unexpected error occurred while sending guest invitations.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }
    }
}
