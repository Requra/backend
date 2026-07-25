using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Participant
{
    public class ParticipantConsentDto
    {
        public bool RecordingConsent { get; set; }
        public DateTime? ConsentedAt { get; set; }
    }

    public class MeetingParticipantResponse
    {
        public Guid Id { get; set; }
        public Guid MeetingId { get; set; }
        public string? UserId { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string Role { get; set; } = null!;
        public string Status { get; set; } = null!;
        public ParticipantConsentDto Consent { get; set; } = new();
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
    }
}
