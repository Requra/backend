using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Meeting
{
    public class MeetingDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; }
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
    }
}
