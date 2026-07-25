using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Participant
{
    public class JoinMeetingRequest
    {
        public Guid MeetingId { get; set; }
        public string? CurrentUserId { get; set; }

        // Used for guest joins (no CurrentUserId). For authenticated joins these are
        // resolved from the user's account instead and any values sent here are ignored.
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
    }

    public class LeaveMeetingRequest
    {
        public Guid MeetingId { get; set; }
        public string? CurrentUserId { get; set; }

        // Lets an authenticated user leave on behalf of a specific participant row
        public Guid? ParticipantId { get; set; }
    }

    public class RemoveParticipantRequest
    {
        public Guid MeetingId { get; set; }
        public Guid ParticipantId { get; set; }
        public string CurrentUserId { get; set; } = null!;
    }

    public class SaveConsentRequest
    {
        public Guid MeetingId { get; set; }
        public Guid ParticipantId { get; set; }
        public string? CurrentUserId { get; set; }
        public bool RecordingConsent { get; set; }
    }
}
