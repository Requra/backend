using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Meeting
{
    public class MeetingDetailsDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string JoinUrl { get; set; } = null!;
        public string CreatedById { get; set; } = null!;
        public string HostParticipantId { get; set; }

        public DateTime? ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int ParticipantsCount { get; set; }
        public Guid? ActiveRecordingId { get; set; }

        public string CurrentUserRole { get; set; } = null!;
        public bool CanStart { get; set; }
        public bool CanEnd { get; set; }
        public bool CanInvite { get; set; }
        public bool CanRecord { get; set; }
    }
}
