using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Meeting
{
    public class EndMeetingResponse
    {
        public Guid MeetingId { get; set; }
        public string PreviousStatus { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
