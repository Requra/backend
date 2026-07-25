using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Participant
{
    public class JoinMeetingApiRequest
    {
        // Required for guest joins 
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
    }

    public class LeaveMeetingApiRequest
    {
        public Guid? ParticipantId { get; set; }
    }

    public class SaveConsentApiRequest
    {
        public bool RecordingConsent { get; set; }
    }
}
