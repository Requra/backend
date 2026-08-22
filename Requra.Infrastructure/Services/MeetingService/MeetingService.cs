using AgoraIO.Media;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentValidation;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Ocsp;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Agora;
using Requra.Application.DTOs.Invitation.MeetingInvitation;
using Requra.Application.DTOs.LiveKit;
using Requra.Application.DTOs.Meeting;
using Requra.Application.DTOs.Participant;
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
using Requra.Infrastructure.Options;
using Requra.Infrastructure.Services.InvitationService.MeetingInvitationService;
using Requra.Infrastructure.Services.MeetingService.AgoraTokenService;
using System.Security.Claims;

namespace Requra.Infrastructure.Services.MeetingService
{
    public class MeetingService(RequraDbContext _context, IValidator<CreateMeetingRequest> _validator, ILogger<MeetingService> _logger, IMapper _mapper, IEmailSender _emailSender, IOptions<LiveKitOptions> _liveKitOptions,IOptions<MeetingOptions> _meetingOptions, IOptions<AgoraOptions> _agoraOptions, IOptions<MeetingInvitationLinkOptions> _meetingInvitationLinkOptions) : IMeetingService
    {

        public async Task<Response<MeetingDto>> CreateMeetingAsync(Guid projectId,CreateMeetingRequest request,string currentUserId)
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

                var joinUrl = $"https://requra-ai.vercel.app/meetings/{meeting.Id}/join";
                meeting.SetPlatform(joinUrl);
                //updated here 
                var hostUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);

                //var hostParticipant = new MeetingParticipant(
                //    currentUserId,
                //    meeting.Id,
                //    MeetingRole.Host
                //);
                var hostParticipant = new MeetingParticipant(
                   meeting.Id,
                   currentUserId,
                   hostUser?.FullName ?? hostUser?.UserName,
                   hostUser?.Email,
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
        public async Task<Response<PagedResult<ProjectMeetingsDto>>> GetMeetingsAsync(Guid projectId, string currentUserId,GetMeetingsQuery query)
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
        JoinUrl = $"https://requra-ai.vercel.app/meetings/{m.Id}/join",
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
        public async Task<Response<MeetingDetailsDto>> GetMeetingByIdAsync(Guid meetingId,string currentUserId)
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
                    JoinUrl = $"https://requra-ai.runasp.net/meetings/{meeting.Id}/join",
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

        public async Task<Response<MeetingDto>> CancelMeetingAsync(Guid meetingId,string currentUserId)
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
                    JoinUrl = $"https://requra-ai.vercel.app/meetings/{meeting.Id}/join",
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

        public async Task<Response<MeetingDto>> UpdateMeetingAsync(Guid meetingId,UpdateMeetingRequest request,string currentUserId)
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
                JoinUrl = $"https://requra-ai.vercel.app/meetings/{meeting.Id}/join",
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
                        var MeetingUrl = BuildMeetingInvitationUrl(
                            meeting.Id,
                            invitation.InviteToken,
                            isGuest: false,
                            platform: request.Platform);

                        var body = MeetingInvitationTemplete.MeetingInvitationEmail(
                            userName: invitation.DisplayName ?? "User",
                            meetingTitle: meeting.Title ?? "Meeting",
                            inviteType: invitation.InviteType.ToString().ToUpper(),
                            meetingRole: invitation.Role.ToString().ToUpper(),
                            scheduledAt: meeting.ScheduledAt,
                            expiresAt: invitation.ExpiresAt,
                            meetingUrl: MeetingUrl,
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
                        var MeetingUrl = BuildMeetingInvitationUrl(meeting.Id, invitation.InviteToken, isGuest: true, platform: request.Platform);


                        var body = MeetingInvitationTemplete.MeetingInvitationEmail(
                            userName: invitation.DisplayName ?? "Guest",
                            meetingTitle: meeting.Title ?? "Meeting",
                            inviteType: invitation.InviteType.ToString().ToUpper(),
                            meetingRole: invitation.Role.ToString().ToUpper(),
                            scheduledAt: meeting.ScheduledAt,
                            expiresAt: invitation.ExpiresAt,
                            meetingUrl: MeetingUrl,
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
        public async Task<Response<PagedResult<MeetingInvitationItemResponse>>> GetMeetingInvitationsAsync( Guid meetingId, string currentUserId, GetMeetingInvitationsQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                var meeting = await _context.MeetingSessions
                    .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);

                if (meeting == null)
                    return Response<PagedResult<MeetingInvitationItemResponse>>.Failure("Meeting not found", StatusCodes.Status404NotFound);

                // Anyone who is already a participant of this meeting can list its invitations.
                var isParticipant = await _context.MeetingParticipants
                    .AnyAsync(p => p.MeetingId == meetingId && p.UserId == currentUserId, cancellationToken);

                if (!isParticipant)
                    return Response<PagedResult<MeetingInvitationItemResponse>>.Failure("You are not allowed to access this meeting", StatusCodes.Status403Forbidden);

                var baseQuery = _context.Invitations.Where(i => i.MeetingId == meetingId);

                if (query.Status.HasValue)
                    baseQuery = baseQuery.Where(i => i.Status == query.Status.Value);

                var totalCount = await baseQuery.CountAsync(cancellationToken);

                var invitations = await baseQuery
                    .OrderByDescending(i => i.CreatedAt)
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(cancellationToken);

                var items = invitations.Select(MapToItemResponse).ToList();

                var result = new PagedResult<MeetingInvitationItemResponse>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };

                return Response<PagedResult<MeetingInvitationItemResponse>>.Success(
                    result,
                    totalCount > 0 ? "Invitations retrieved successfully" : "No invitations found",
                    StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving meeting invitations");
                return Response<PagedResult<MeetingInvitationItemResponse>>.Failure(
                    "An error occurred while retrieving meeting invitations",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }

        public async Task<Response<MeetingInvitationPreviewResponse>> PreviewInvitationAsync(string inviteToken,CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inviteToken))
                return Response<MeetingInvitationPreviewResponse>.Failure("Invalid invite token", StatusCodes.Status422UnprocessableEntity, new List<string> { "inviteToken is required" });

            try
            {
                var invitation = await _context.Invitations
                    .Include(i => i.Meeting)
                        .ThenInclude(m => m.Project)
                    .FirstOrDefaultAsync(i => i.InviteToken == inviteToken, cancellationToken);

                if (invitation == null)
                    return Response<MeetingInvitationPreviewResponse>.Failure("Invitation not found", StatusCodes.Status404NotFound);

                // Lazily flip a stale pending invite to Expired the moment it's looked up.
                if (invitation.Status == InvitationStatus.Pending &&
                    invitation.ExpiresAt.HasValue &&
                    invitation.ExpiresAt.Value < DateTime.UtcNow)
                {
                    invitation.MarkExpired();
                    await _context.SaveChangesAsync(cancellationToken);
                }

                var dto = new MeetingInvitationPreviewResponse
                {
                    MeetingId = invitation.MeetingId ?? Guid.Empty,
                    MeetingTitle = invitation.Meeting?.Title,
                    ProjectName = invitation.Meeting?.Project?.Name,
                    ScheduledAt = invitation.Meeting?.ScheduledAt,
                    InviteeEmail = invitation.Email,
                    InviteeDisplayName = invitation.DisplayName,
                    InviteeType = ToInviteeType(invitation.InviteType),
                    Role = invitation.Role?.ToString().ToUpper(),
                    Status = invitation.Status.ToString().ToUpper(),
                    ExpiresAt = invitation.ExpiresAt
                };

                return Response<MeetingInvitationPreviewResponse>.Success(
                    dto,
                    "Invitation preview retrieved successfully",
                    StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the invitation preview");
                return Response<MeetingInvitationPreviewResponse>.Failure(
                    "An error occurred while retrieving the invitation preview",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }
        public async Task<Response<AcceptMeetingInvitationResponse>> AcceptInvitationAsync(string inviteToken,string? currentUserId,CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inviteToken))
            {
                return Response<AcceptMeetingInvitationResponse>.Failure("Invalid invite token",StatusCodes.Status422UnprocessableEntity,new List<string> { "inviteToken is required" });
            }

            try
            {
                var invitation = await _context.Invitations
                    .Include(i => i.Meeting)
                    .FirstOrDefaultAsync(i => i.InviteToken == inviteToken, cancellationToken);

                if (invitation == null)
                {
                    return Response<AcceptMeetingInvitationResponse>.Failure("Invitation not found",StatusCodes.Status404NotFound);
                }

                if (invitation.Meeting == null)
                {
                    return Response<AcceptMeetingInvitationResponse>.Failure("Meeting not found",StatusCodes.Status404NotFound);
                }

                // Lazily expire stale pending invitations
                if (invitation.Status == InvitationStatus.Pending &&invitation.ExpiresAt.HasValue &&invitation.ExpiresAt.Value < DateTime.UtcNow)
                {
                    invitation.MarkExpired();
                    await _context.SaveChangesAsync(cancellationToken);
                }

                switch (invitation.Status)
                {
                    case InvitationStatus.Accepted:
                        return Response<AcceptMeetingInvitationResponse>.Failure("This invitation has already been accepted",StatusCodes.Status409Conflict);

                    case InvitationStatus.Declined:
                        return Response<AcceptMeetingInvitationResponse>.Failure("This invitation has been declined",StatusCodes.Status409Conflict);

                    case InvitationStatus.Revoked:
                        return Response<AcceptMeetingInvitationResponse>.Failure("This invitation has been revoked",StatusCodes.Status409Conflict);

                    case InvitationStatus.Expired:
                        return Response<AcceptMeetingInvitationResponse>.Failure("This invitation has expired",StatusCodes.Status409Conflict);
                }

                string? participantId = null;

                // AUTHENTICATED USER FLOW
                if (!string.IsNullOrWhiteSpace(currentUserId))
                {
                    var currentUser = await _context.Users.FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);

                    if (currentUser == null)
                    {
                        return Response<AcceptMeetingInvitationResponse>.Failure("Current user not found",StatusCodes.Status404NotFound);
                    }

                    // Accept only if this user is the intended recipient
                    var isIntendedRecipient =
                        (!string.IsNullOrWhiteSpace(invitation.ProjectMemberId) && invitation.ProjectMemberId == currentUserId) ||
                        (!string.IsNullOrWhiteSpace(currentUser.Email) &&string.Equals(currentUser.Email.Trim(), invitation.Email?.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (!isIntendedRecipient)
                    {
                        return Response<AcceptMeetingInvitationResponse>.Failure(
                            "You are not allowed to accept this invitation",
                            StatusCodes.Status403Forbidden);
                    }

                    var existingParticipant = await _context.MeetingParticipants
                        .FirstOrDefaultAsync(
                            p => p.MeetingId == invitation.MeetingId && p.UserId == currentUserId,
                            cancellationToken);

                    if (existingParticipant != null)
                    {
                        participantId = existingParticipant.Id.ToString();
                    }
                    else
                    {
                        var participant = new MeetingParticipant(
                            invitation.MeetingId!.Value,
                            currentUserId,
                            currentUser.FullName ?? currentUser.UserName,
                            currentUser.Email,
                            invitation.Role ?? MeetingRole.Participant);

                        _context.MeetingParticipants.Add(participant);
                        participantId = participant.Id.ToString();
                    }
                }
                else
                {


                    var participant = new MeetingParticipant(
                            invitation.MeetingId!.Value,
                            null,
                            invitation.DisplayName ?? "Unknown",
                            invitation.Email,
                            invitation.Role ?? MeetingRole.Viewer);

                    _context.MeetingParticipants.Add(participant);
                    participantId = participant.Id.ToString();
                }

                invitation.MarkAccepted();
                await _context.SaveChangesAsync(cancellationToken);

                var dto = new AcceptMeetingInvitationResponse
                {
                    InvitationId = invitation.Id,
                    MeetingId = invitation.MeetingId ?? Guid.Empty,
                    Status = invitation.Status.ToString().ToUpper(),
                    ParticipantId = participantId
                };

                return Response<AcceptMeetingInvitationResponse>.Success(dto,"Invitation accepted successfully",StatusCodes.Status200OK);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<AcceptMeetingInvitationResponse>.Failure("A concurrency error occurred while accepting the invitation",StatusCodes.Status409Conflict,new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<AcceptMeetingInvitationResponse>.Failure("A database error occurred while accepting the invitation",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while accepting the invitation");

                return Response<AcceptMeetingInvitationResponse>.Failure("An error occurred while accepting the invitation",StatusCodes.Status500InternalServerError,new List<string> { ex.Message });
            }
        }
        public async Task<Response<MeetingInvitationDetailResponse>> ResendInvitationAsync( Guid meetingId, Guid invitationId, string currentUserId,ClientPlatform? platform=ClientPlatform.Web, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Response<MeetingInvitationDetailResponse>.Failure("Unauthorized User", StatusCodes.Status401Unauthorized);

            if (meetingId == Guid.Empty || invitationId == Guid.Empty)
                return Response<MeetingInvitationDetailResponse>.Failure("Validation failed", StatusCodes.Status422UnprocessableEntity, new List<string> { "Invalid meetingId or invitationId format" });

            try
            {
                var meeting = await _context.MeetingSessions
                    .Include(m => m.Participants)
                    .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);

                if (meeting == null)
                    return Response<MeetingInvitationDetailResponse>.Failure("Meeting not found", StatusCodes.Status404NotFound);

                var currentUserParticipant = meeting.Participants.FirstOrDefault(p => p.UserId == currentUserId);

                if (currentUserParticipant == null)
                    return Response<MeetingInvitationDetailResponse>.Failure("You are not allowed to access this meeting", StatusCodes.Status403Forbidden);

                if (currentUserParticipant.Role != MeetingRole.Host)
                    return Response<MeetingInvitationDetailResponse>.Failure("Only the host can resend invitations", StatusCodes.Status403Forbidden);

                var invitation = await _context.Invitations
                    .FirstOrDefaultAsync(i => i.Id == invitationId && i.MeetingId == meetingId, cancellationToken);

                if (invitation == null)
                    return Response<MeetingInvitationDetailResponse>.Failure("Invitation not found", StatusCodes.Status404NotFound);

                // Lazily flip a stale pending invite to Expired the moment it's looked up.
                if (invitation.Status == InvitationStatus.Pending &&
                    invitation.ExpiresAt.HasValue &&
                    invitation.ExpiresAt.Value < DateTime.UtcNow)
                {
                    invitation.MarkExpired();
                }

                switch (invitation.Status)
                {
                    case InvitationStatus.Accepted:
                        return Response<MeetingInvitationDetailResponse>.Failure("Cannot resend an invitation that has already been accepted", StatusCodes.Status409Conflict);
                    case InvitationStatus.Declined:
                        return Response<MeetingInvitationDetailResponse>.Failure("Cannot resend an invitation that was declined", StatusCodes.Status409Conflict);
                    case InvitationStatus.Revoked:
                        return Response<MeetingInvitationDetailResponse>.Failure("Cannot resend an invitation that has been revoked", StatusCodes.Status409Conflict);
                }

                // Pending or Expired invitations can be resent: refresh the expiry window.
                invitation.Resend(DateTime.UtcNow.AddDays(3));

                await _context.SaveChangesAsync(cancellationToken);

                try
                {
                    var currentUser = await _context.Users.FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);

                    var subject = $"Invitation to meeting: {meeting.Title ?? "Meeting"}";
                    var IsGuest = invitation.InviteType == InviteType.Guest;
                    var MeetingUrl = BuildMeetingInvitationUrl(meeting.Id, invitation.InviteToken, isGuest: IsGuest, platform: platform);

                    var body = MeetingInvitationTemplete.MeetingInvitationEmail(
                        userName: invitation.DisplayName ?? "User",
                        meetingTitle: meeting.Title ?? "Meeting",
                        inviteType: invitation.InviteType.ToString().ToUpper(),
                        meetingRole: invitation.Role.ToString().ToUpper(),
                        scheduledAt: meeting.ScheduledAt,
                        expiresAt: invitation.ExpiresAt,
                        meetingUrl: MeetingUrl,
                        invitedByName: currentUser?.FullName ?? currentUser?.UserName ?? "Requra Team",
                        meetingDescription: meeting.Description,
                        isGuest: invitation.InviteType == InviteType.Guest);

                    await _emailSender.SendEmailAsync(invitation.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resend invitation email to {Email}", invitation.Email);
                }

                return Response<MeetingInvitationDetailResponse>.Success(
                    MapToDetailResponse(invitation),
                    "Invitation resent successfully",
                    StatusCodes.Status200OK);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<MeetingInvitationDetailResponse>.Failure(
                    "A concurrency error occurred while resending the invitation",
                    StatusCodes.Status409Conflict,
                    new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<MeetingInvitationDetailResponse>.Failure(
                    "A database error occurred while resending the invitation",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resending the invitation");
                return Response<MeetingInvitationDetailResponse>.Failure(
                    "An error occurred while resending the invitation",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }

        public async Task<Response<MeetingInvitationDetailResponse>> RevokeInvitationAsync( Guid meetingId,   Guid invitationId,   string currentUserId,  CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Response<MeetingInvitationDetailResponse>.Failure("Unauthorized User", StatusCodes.Status401Unauthorized);

            if (meetingId == Guid.Empty || invitationId == Guid.Empty)
                return Response<MeetingInvitationDetailResponse>.Failure("Validation failed", StatusCodes.Status422UnprocessableEntity, new List<string> { "Invalid meetingId or invitationId format" });

            try
            {
                var meeting = await _context.MeetingSessions
                    .Include(m => m.Participants)
                    .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);

                if (meeting == null)
                    return Response<MeetingInvitationDetailResponse>.Failure("Meeting not found", StatusCodes.Status404NotFound);

                var currentUserParticipant = meeting.Participants.FirstOrDefault(p => p.UserId == currentUserId);

                if (currentUserParticipant == null)
                    return Response<MeetingInvitationDetailResponse>.Failure("You are not allowed to access this meeting", StatusCodes.Status403Forbidden);

                if (currentUserParticipant.Role != MeetingRole.Host)
                    return Response<MeetingInvitationDetailResponse>.Failure("Only the host can revoke invitations", StatusCodes.Status403Forbidden);

                var invitation = await _context.Invitations
                    .FirstOrDefaultAsync(i => i.Id == invitationId && i.MeetingId == meetingId, cancellationToken);

                if (invitation == null)
                    return Response<MeetingInvitationDetailResponse>.Failure("Invitation not found", StatusCodes.Status404NotFound);

                switch (invitation.Status)
                {
                    case InvitationStatus.Revoked:
                        return Response<MeetingInvitationDetailResponse>.Failure("This invitation has already been revoked", StatusCodes.Status409Conflict);
                    case InvitationStatus.Accepted:
                        return Response<MeetingInvitationDetailResponse>.Failure("Cannot revoke an invitation that has already been accepted", StatusCodes.Status409Conflict);
                    case InvitationStatus.Declined:
                        return Response<MeetingInvitationDetailResponse>.Failure("Cannot revoke an invitation that was declined", StatusCodes.Status409Conflict);
                }

                invitation.MarkRevoked();

                await _context.SaveChangesAsync(cancellationToken);

                return Response<MeetingInvitationDetailResponse>.Success(
                    MapToDetailResponse(invitation),
                    "Invitation revoked successfully",
                    StatusCodes.Status200OK);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Response<MeetingInvitationDetailResponse>.Failure(
                    "A concurrency error occurred while revoking the invitation",
                    StatusCodes.Status409Conflict,
                    new List<string> { ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Response<MeetingInvitationDetailResponse>.Failure(
                    "A database error occurred while revoking the invitation",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while revoking the invitation");
                return Response<MeetingInvitationDetailResponse>.Failure(
                    "An error occurred while revoking the invitation",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }


        // Participant
        public async Task<Response<MeetingParticipantResponse>> JoinMeetingAsync(JoinMeetingRequest request,CancellationToken cancellationToken = default)
        {
            try
            {
                var meeting = await _context.MeetingSessions
                    .Include(m => m.Participants)
                    .FirstOrDefaultAsync(m => m.Id == request.MeetingId, cancellationToken);

                if (meeting == null)
                    return Response<MeetingParticipantResponse>.Failure("Meeting not found", StatusCodes.Status404NotFound);

                if (meeting.Status == MeetingStatus.Ended || meeting.Status == MeetingStatus.Cancelled)
                    return Response<MeetingParticipantResponse>.Failure(
                        $"Meeting cannot be joined because it is {meeting.Status}",
                        StatusCodes.Status409Conflict);

                var isGuestJoin = string.IsNullOrEmpty(request.CurrentUserId);

                ApplicationUser? currentUser = null;

                if (!isGuestJoin)
                {
                    // Authenticated join: caller must be a project member to join directly.
                    var isMember = await _context.ProjectMembers
                        .AnyAsync(pm => pm.ProjectId == meeting.ProjectId && pm.UserId == request.CurrentUserId, cancellationToken);

                    if (!isMember)
                        return Response<MeetingParticipantResponse>.Failure("You are not allowed to join this meeting", StatusCodes.Status403Forbidden);

                    currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.CurrentUserId, cancellationToken);
                }
                else
                {
                    // Guest join: displayName/email are required since there's no account to resolve them from.
                    if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Email))
                        return Response<MeetingParticipantResponse>.Failure(
                            "Guest join requires displayName and email",
                            StatusCodes.Status422UnprocessableEntity,
                            new List<string> { "displayName and email are required for guest joins" });
                }

                MeetingParticipant participant;

                if (!isGuestJoin)
                {
                    // Rejoin the same row if this user already has one (e.g. they left earlier),
                    // instead of creating duplicate participant history for the same account.
                    var existing = meeting.Participants.FirstOrDefault(p => p.UserId == request.CurrentUserId);

                    if (existing != null)
                    {
                        if (existing.Status == ParticipantStatus.Removed)
                            return Response<MeetingParticipantResponse>.Failure("You have been removed from this meeting", StatusCodes.Status403Forbidden);

                        if (existing.Status != ParticipantStatus.Joined)
                            existing.Rejoin();

                        participant = existing;
                    }
                    else
                    {
                        var role = meeting.HostId == request.CurrentUserId ? MeetingRole.Host : MeetingRole.Participant;

                        participant = new MeetingParticipant(
                            meeting.Id,
                            request.CurrentUserId,
                            currentUser?.FullName ?? currentUser?.UserName,
                            currentUser?.Email,
                            role);

                        _context.MeetingParticipants.Add(participant);
                    }
                }
                else
                {
                    // Guests always get a fresh participant row — there's no stable identity
                    // to dedupe/rejoin against.
                    participant = new MeetingParticipant(
                        meeting.Id,
                        null,
                        request.DisplayName,
                        request.Email,
                        MeetingRole.Participant);

                    _context.MeetingParticipants.Add(participant);
                }

                await _context.SaveChangesAsync(cancellationToken);

                return Response<MeetingParticipantResponse>.Success(
                    MapToParticipantResponse(participant),
                    "Joined meeting successfully",
                    StatusCodes.Status200OK);
            }
            catch (DbUpdateException ex)
            {
                return Response<MeetingParticipantResponse>.Failure("A database error occurred while joining the meeting.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while joining the meeting");
                return Response<MeetingParticipantResponse>.Failure(
                    "An error occurred while joining the meeting",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }

        public async Task<Response<MeetingParticipantResponse>> LeaveMeetingAsync(LeaveMeetingRequest request,CancellationToken cancellationToken = default)
        {
            try
            {
                var meeting = await _context.MeetingSessions
                    .Include(m => m.Participants)
                    .FirstOrDefaultAsync(m => m.Id == request.MeetingId, cancellationToken);

                if (meeting == null)
                    return Response<MeetingParticipantResponse>.Failure("Meeting not found", StatusCodes.Status404NotFound);

                MeetingParticipant? participant = null;

                if (request.ParticipantId.HasValue)
                {
                    participant = meeting.Participants.FirstOrDefault(p => p.Id == request.ParticipantId.Value);
                }
                else if (!string.IsNullOrEmpty(request.CurrentUserId))
                {
                    participant = meeting.Participants
                        .Where(p => p.UserId == request.CurrentUserId)
                        .OrderByDescending(p => p.JoinedAt)
                        .FirstOrDefault();
                }
                else
                {
                    return Response<MeetingParticipantResponse>.Failure(
                        "participantId is required to leave without an authenticated session",
                        StatusCodes.Status422UnprocessableEntity,
                        new List<string> { "participantId is required" });
                }

                if (participant == null)
                    return Response<MeetingParticipantResponse>.Failure("Participant not found", StatusCodes.Status404NotFound);

                // A caller can only leave on someone else's behalf if they're the host;
                // otherwise you can only leave your own session.
                if (request.ParticipantId.HasValue &&
                    participant.UserId != request.CurrentUserId &&
                    meeting.HostId != request.CurrentUserId)
                {
                    return Response<MeetingParticipantResponse>.Failure("You are not allowed to remove this participant", StatusCodes.Status403Forbidden);
                }

                if (participant.Status == ParticipantStatus.Left)
                    return Response<MeetingParticipantResponse>.Failure("Participant has already left this meeting", StatusCodes.Status409Conflict);

                if (participant.Status == ParticipantStatus.Removed)
                    return Response<MeetingParticipantResponse>.Failure("Participant was already removed from this meeting", StatusCodes.Status409Conflict);

                participant.MarkLeft();
                await _context.SaveChangesAsync(cancellationToken);

                return Response<MeetingParticipantResponse>.Success(
                    MapToParticipantResponse(participant),
                    "Left meeting successfully",
                    StatusCodes.Status200OK);
            }
            catch (DbUpdateException ex)
            {
                return Response<MeetingParticipantResponse>.Failure("A database error occurred while leaving the meeting.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while leaving the meeting");
                return Response<MeetingParticipantResponse>.Failure(
                    "An error occurred while leaving the meeting",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }

        public async Task<Response<PagedResult<MeetingParticipantResponse>>> GetMeetingParticipantsAsync(Guid meetingId,string currentUserId,GetMeetingParticipantsQuery query,CancellationToken cancellationToken = default)
        {
            try
            {
                var meeting = await _context.MeetingSessions
                    .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);

                if (meeting == null)
                    return Response<PagedResult<MeetingParticipantResponse>>.Failure("Meeting not found", StatusCodes.Status404NotFound);

                var isParticipant = await _context.MeetingParticipants
                    .AnyAsync(p => p.MeetingId == meetingId && p.UserId == currentUserId, cancellationToken);

                if (!isParticipant)
                    return Response<PagedResult<MeetingParticipantResponse>>.Failure("You are not allowed to access this meeting", StatusCodes.Status403Forbidden);

                var baseQuery = _context.MeetingParticipants.Where(p => p.MeetingId == meetingId);

                var totalCount = await baseQuery.CountAsync(cancellationToken);

                var participants = await baseQuery
                    .OrderBy(p => p.JoinedAt)
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(cancellationToken);

                var items = participants.Select(MapToParticipantResponse).ToList();

                var result = new PagedResult<MeetingParticipantResponse>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };

                return Response<PagedResult<MeetingParticipantResponse>>.Success(
                    result,
                    totalCount > 0 ? "Participants retrieved successfully" : "No participants found",
                    StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving meeting participants");
                return Response<PagedResult<MeetingParticipantResponse>>.Failure(
                    "An error occurred while retrieving meeting participants",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }

        public async Task<Response<MeetingParticipantResponse>> RemoveParticipantAsync(RemoveParticipantRequest request,CancellationToken cancellationToken = default)
        {
            try
            {
                var meeting = await _context.MeetingSessions
                    .Include(m => m.Participants)
                    .FirstOrDefaultAsync(m => m.Id == request.MeetingId, cancellationToken);

                if (meeting == null)
                    return Response<MeetingParticipantResponse>.Failure("Meeting not found", StatusCodes.Status404NotFound);

                // Only the host can remove participants.
                if (meeting.HostId != request.CurrentUserId)
                    return Response<MeetingParticipantResponse>.Failure("Only the host can remove participants", StatusCodes.Status403Forbidden);

                var participant = meeting.Participants.FirstOrDefault(p => p.Id == request.ParticipantId);

                if (participant == null)
                    return Response<MeetingParticipantResponse>.Failure("Participant not found", StatusCodes.Status404NotFound);

                if (participant.Role == MeetingRole.Host)
                    return Response<MeetingParticipantResponse>.Failure("The host cannot be removed from their own meeting", StatusCodes.Status400BadRequest);

                if (participant.Status == ParticipantStatus.Removed)
                    return Response<MeetingParticipantResponse>.Failure("Participant was already removed from this meeting", StatusCodes.Status409Conflict);

                participant.MarkRemoved();
                await _context.SaveChangesAsync(cancellationToken);

                return Response<MeetingParticipantResponse>.Success(
                    MapToParticipantResponse(participant),
                    "Participant removed successfully",
                    StatusCodes.Status200OK);
            }
            catch (DbUpdateException ex)
            {
                return Response<MeetingParticipantResponse>.Failure("A database error occurred while removing the participant.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while removing the participant");
                return Response<MeetingParticipantResponse>.Failure(
                    "An error occurred while removing the participant",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }

        public async Task<Response<MeetingParticipantResponse>> SaveConsentAsync(SaveConsentRequest request,CancellationToken cancellationToken = default)
        {
            try
            {
                var meeting = await _context.MeetingSessions
                    .Include(m => m.Participants)
                    .FirstOrDefaultAsync(m => m.Id == request.MeetingId, cancellationToken);

                if (meeting == null)
                    return Response<MeetingParticipantResponse>.Failure("Meeting not found", StatusCodes.Status404NotFound);

                var participant = meeting.Participants.FirstOrDefault(p => p.Id == request.ParticipantId);

                if (participant == null)
                    return Response<MeetingParticipantResponse>.Failure("Participant not found", StatusCodes.Status404NotFound);

                // A participant can only set their own consent; the host can set it for
                // anyone (e.g. recording a guest who confirmed verbally).
                var isSelf = !string.IsNullOrEmpty(request.CurrentUserId) && participant.UserId == request.CurrentUserId;
                var isHost = !string.IsNullOrEmpty(request.CurrentUserId) && meeting.HostId == request.CurrentUserId;
                //var isGuest = participant.Id == request.ParticipantId;


                //if (!isSelf && !isHost && !isGuest)
                //    return Response<MeetingParticipantResponse>.Failure("You are not allowed to set consent for this participant", StatusCodes.Status403Forbidden);

                if (participant.Status == ParticipantStatus.Removed)
                    return Response<MeetingParticipantResponse>.Failure("Cannot set consent for a removed participant", StatusCodes.Status409Conflict);

                participant.SetConsent(request.RecordingConsent);
                await _context.SaveChangesAsync(cancellationToken);

                return Response<MeetingParticipantResponse>.Success(
                    MapToParticipantResponse(participant),
                    "Recording consent saved successfully",
                    StatusCodes.Status200OK);
            }
            catch (DbUpdateException ex)
            {
                return Response<MeetingParticipantResponse>.Failure("A database error occurred while saving consent.", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving recording consent");
                return Response<MeetingParticipantResponse>.Failure(
                    "An error occurred while saving recording consent",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }


        //LiveKit
        public async Task<Response<LiveKitTokenResponseDto>> IssueTokenAsync(Guid meetingId,string? callerUserId,Guid? participantId,CancellationToken cancellationToken = default)
        {
            //if (string.IsNullOrWhiteSpace(callerUserId))
            //{
            //    return Response<LiveKitTokenResponseDto>.Failure("Caller identity could not be resolved from the token.",StatusCodes.Status401Unauthorized);
            //}

            var meeting = await _context.MeetingSessions
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);

            if (meeting is null)
            {
                return Response<LiveKitTokenResponseDto>.Failure("Meeting not found.",StatusCodes.Status404NotFound);
            }

            if (meeting.Status != MeetingStatus.Live)
            {
                return Response<LiveKitTokenResponseDto>.Failure("Meeting is not currently live.",StatusCodes.Status409Conflict
                );
            }

            if (meeting.MeetingEndsAt.HasValue && DateTime.UtcNow >= meeting.MeetingEndsAt.Value)
            {
                return Response<LiveKitTokenResponseDto>.Failure("The meeting's live window has ended.",StatusCodes.Status409Conflict);
            }

            // 2. Resolve participant row: explicit participantId, or unambiguous derivation.
            MeetingParticipant? participant;

            if (participantId.HasValue)
            {
                participant = meeting.Participants
                    .FirstOrDefault(p => p.Id == participantId.Value);

                if (participant is null)
                {
                    return Response<LiveKitTokenResponseDto>.Failure("Participant not found for this meeting.", StatusCodes.Status403Forbidden);
                }

                //if (participant.UserId != callerUserId)
                //{
                //    return Response<LiveKitTokenResponseDto>.Failure("Caller does not own this participant record.", StatusCodes.Status403Forbidden);
                //}
            }
            else
            {
                // Only safe when caller has exactly one joined participant row in this meeting.
                var candidates = meeting.Participants
                    .Where(p => p.UserId == callerUserId && p.Status == ParticipantStatus.Joined)
                    .ToList();

                if (candidates.Count != 1)
                {
                    return Response<LiveKitTokenResponseDto>.Failure("Could not unambiguously resolve the caller's participant record; pass participantId explicitly.",StatusCodes.Status403Forbidden);
                }

                participant = candidates[0];
            }

            if (participant.Status != ParticipantStatus.Joined)
            {
                return Response<LiveKitTokenResponseDto>.Failure("Participant has not joined the meeting.", StatusCodes.Status403Forbidden);
            }

            // NOTE: intentionally no recordingConsent check here — enforced only at recording start.

            DateTime expiresAt;
            try
            {
                var roomName = $"requra-meeting-{meeting.Id}";
                var identity = $"requra-participant-{participant.Id}";

                var maxByConfig = DateTime.UtcNow.AddMinutes(_meetingOptions.Value.MvpMaxLiveDurationMinutes);
                expiresAt = meeting.MeetingEndsAt.HasValue && meeting.MeetingEndsAt.Value < maxByConfig
                    ? meeting.MeetingEndsAt.Value
                    : maxByConfig;

                var ttl = expiresAt - DateTime.UtcNow;
                if (ttl <= TimeSpan.Zero)
                {
                    return Response<LiveKitTokenResponseDto>.Failure("The meeting's live window has ended.",StatusCodes.Status409Conflict);
                }

                var accessToken = new Livekit.Server.Sdk.Dotnet.AccessToken(_liveKitOptions.Value.ApiKey, _liveKitOptions.Value.ApiSecret)
                    .WithIdentity(identity)
                    .WithTtl(ttl)
                    .WithGrants(new VideoGrants
                    {
                        RoomJoin = true,
                        Room = roomName,
                        CanPublish = true,
                        CanSubscribe = true
                    }).WithName(string.IsNullOrWhiteSpace(participant.DisplayName) ? "Guest" : participant.DisplayName.Trim())
                    .WithMetadata($"{{\"meetingId\":\"{meeting.Id}\",\"participantId\":\"{participant.Id}\",\"role\":\"{participant.Role}\"}}");

                var token = accessToken.ToJwt();

                var response = new LiveKitTokenResponseDto
                {
                    ServerUrl = _liveKitOptions.Value.Url,
                    Token = token,
                    RoomName = roomName,
                    Identity = identity,
                    ExpiresAt = expiresAt,
                    MeetingEndsAt = meeting.MeetingEndsAt ?? expiresAt,
                    DisplayName = participant.DisplayName ?? "Guest"
                };

                return Response<LiveKitTokenResponseDto>.Success(response, "Live meeting credentials issued");
            }
            catch (Exception ex)
            {
                return Response<LiveKitTokenResponseDto>.Failure("Unable to issue live meeting credentials at this time.",StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Response<AgoraRtcTokenResponseDto>> IssueAgoraTokenAsync(Guid meetingId,string? callerUserId,Guid? participantId,CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(callerUserId))
            {
                return Response<AgoraRtcTokenResponseDto>.Failure(
                    "Caller identity could not be resolved from the token.",
                    StatusCodes.Status401Unauthorized);
            }

            if (string.IsNullOrWhiteSpace(_agoraOptions.Value.AppId) ||
                string.IsNullOrWhiteSpace(_agoraOptions.Value.AppCertificate))
            {
                return Response<AgoraRtcTokenResponseDto>.Failure(
                    "Agora provider is not configured.",
                    StatusCodes.Status500InternalServerError);
            }

            var meeting = await _context.MeetingSessions
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);

            if (meeting is null)
            {
                return Response<AgoraRtcTokenResponseDto>.Failure(
                    "Meeting not found.",
                    StatusCodes.Status404NotFound);
            }

            if (meeting.Status != MeetingStatus.Live)
            {
                return Response<AgoraRtcTokenResponseDto>.Failure(
                    "Meeting is not currently live.",
                    StatusCodes.Status409Conflict);
            }

            if (meeting.MeetingEndsAt.HasValue && DateTime.UtcNow >= meeting.MeetingEndsAt.Value)
            {
                return Response<AgoraRtcTokenResponseDto>.Failure(
                    "The meeting's live window has ended.",
                    StatusCodes.Status409Conflict);
            }

            MeetingParticipant? participant;

            if (participantId.HasValue)
            {
                participant = meeting.Participants
                    .FirstOrDefault(p => p.Id == participantId.Value);

                if (participant is null)
                {
                    return Response<AgoraRtcTokenResponseDto>.Failure(
                        "Participant not found for this meeting.",
                        StatusCodes.Status403Forbidden);
                }

                if (participant.UserId != callerUserId)
                {
                    return Response<AgoraRtcTokenResponseDto>.Failure(
                        "Caller does not own this participant record.",
                        StatusCodes.Status403Forbidden);
                }
            }
            else
            {
                var candidates = meeting.Participants
                    .Where(p => p.UserId == callerUserId && p.Status == ParticipantStatus.Joined)
                    .ToList();

                if (candidates.Count != 1)
                {
                    return Response<AgoraRtcTokenResponseDto>.Failure(
                        "Could not unambiguously resolve the caller's participant record; pass participantId explicitly.",
                        StatusCodes.Status403Forbidden);
                }

                participant = candidates[0];
            }

            if (participant.Status != ParticipantStatus.Joined)
            {
                return Response<AgoraRtcTokenResponseDto>.Failure(
                    "Participant has not joined the meeting.",
                    StatusCodes.Status403Forbidden);
            }

            try
            {
                var channelName = meeting.Id.ToString();
                var uid = participant.Id.ToString();

                var expirationSeconds = _agoraOptions.Value.TokenExpirationSeconds > 0
                    ? _agoraOptions.Value.TokenExpirationSeconds
                    : 3600;

                var expiresAt = DateTime.UtcNow.AddSeconds(expirationSeconds);

                if (meeting.MeetingEndsAt.HasValue && meeting.MeetingEndsAt.Value < expiresAt)
                {
                    expiresAt = meeting.MeetingEndsAt.Value;
                }

                var ttl = expiresAt - DateTime.UtcNow;
                if (ttl <= TimeSpan.Zero)
                {
                    return Response<AgoraRtcTokenResponseDto>.Failure(
                        "The meeting's live window has ended.",
                        StatusCodes.Status409Conflict);
                }

                var privilegeExpireTs = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();

                var agoraRole = participant.Role == MeetingRole.Viewer
                    ? AgoraIO.Media.RtcTokenBuilder2.Role.RoleSubscriber
                    : AgoraIO.Media.RtcTokenBuilder2.Role.RolePublisher;
                var expireInSeconds = (uint)Math.Max(1, (int)Math.Ceiling(ttl.TotalSeconds));
                var token = RtcTokenBuilder2.buildTokenWithUserAccount(
                    _agoraOptions.Value.AppId,
                    _agoraOptions.Value.AppCertificate,
                    channelName,
                    uid,
                    agoraRole,
                    expireInSeconds,
                    expireInSeconds);

                var response = new AgoraRtcTokenResponseDto
                {
                    AppId = _agoraOptions.Value.AppId,
                    ChannelName = channelName,
                    Uid = uid,
                    Token = token,
                    Role = agoraRole == AgoraIO.Media.RtcTokenBuilder2.Role.RolePublisher ? "PUBLISHER" : "SUBSCRIBER",
                    ExpiresAt = expiresAt
                };

                return Response<AgoraRtcTokenResponseDto>.Success(
                    response,
                    "Agora RTC token issued successfully.");
            }
            catch (Exception)
            {
                return Response<AgoraRtcTokenResponseDto>.Failure(
                    "Unable to issue Agora RTC token at this time.",
                    StatusCodes.Status500InternalServerError);
            }
        }
        private static MeetingParticipantResponse MapToParticipantResponse(MeetingParticipant participant)
        {
            return new MeetingParticipantResponse
            {
                Id = participant.Id,
                MeetingId = participant.MeetingId,
                UserId = participant.UserId,
                DisplayName = participant.DisplayName,
                Email = participant.Email,
                Role = participant.Role.ToString(),
                Status = participant.Status.ToString(),
                Consent = new ParticipantConsentDto
                {
                    RecordingConsent = participant.RecordingConsent,
                    ConsentedAt = participant.ConsentedAt
                },
                JoinedAt = participant.JoinedAt,
                LeftAt = participant.LeftAt
            };
        }
        private static MeetingInvitationDetailResponse MapToDetailResponse(Invitation invitation)
        {
            return new MeetingInvitationDetailResponse
            {
                Id = invitation.Id,
                MeetingId = invitation.MeetingId ?? Guid.Empty,
                InviteeType = ToInviteeType(invitation.InviteType),
                Email = invitation.Email,
                DisplayName = invitation.DisplayName,
                ProjectMemberId = invitation.ProjectMemberId,
                StakeholderId = invitation.StakeholderId,
                Role = (invitation.Role ?? MeetingRole.Participant).ToString().ToUpper(),
                Status = invitation.Status.ToString().ToUpper(),
                InvitedById = invitation.InvitedById,
                ExpiresAt = invitation.ExpiresAt,
                CreatedAt = invitation.CreatedAt
            };
        }

        // mapping between
        // invitation entity and  representation.
        private static MeetingInvitationItemResponse MapToItemResponse(Invitation invitation)
        {
            return new MeetingInvitationItemResponse
            {
                Id = invitation.Id,
                MeetingId = invitation.MeetingId ?? Guid.Empty,
                InviteType = invitation.InviteType ?? InviteType.Guest,
                Email = invitation.Email,
                DisplayName = invitation.DisplayName,
                ProjectMemberId = invitation.ProjectMemberId,
                StakeholderId = invitation.StakeholderId,
                Role = invitation.Role ?? MeetingRole.Participant,
                Status = invitation.Status,
                InvitedById = invitation.InvitedById,
                ExpiresAt = invitation.ExpiresAt,
                CreatedAt = invitation.CreatedAt
            };
        }

        private static string ToInviteeType(InviteType? inviteType)
        {
            return inviteType == InviteType.Participant ? "PARTICIPANT" : "GUEST";
        }

        private string BuildMeetingInvitationUrl(Guid meetingId,string inviteToken,bool isGuest, ClientPlatform? platform = ClientPlatform.Web)
        {
            if (platform == ClientPlatform.Mobile)
            {
                var baseUrl = _meetingInvitationLinkOptions.Value.MobileAppLinkBaseUrl.TrimEnd('/');

                if (isGuest)
                {
                    return $"https://requra-ai.runasp.net/meeting/join?meetingId={meetingId}&guestToken={Uri.EscapeDataString(inviteToken)}";
                }

                return $"https://requra-ai.runasp.net/meeting/join?meetingId={meetingId}&Token={Uri.EscapeDataString(inviteToken)}";
            }
            else
            {
                var baseUrl = _meetingInvitationLinkOptions.Value.WebBaseUrl.TrimEnd('/');

                return $"https://requra-ai.vercel.app/invite/{Uri.EscapeDataString(inviteToken)}";
            }
        }
    }
}
