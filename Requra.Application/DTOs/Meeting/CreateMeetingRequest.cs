using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Meeting
{
    public class CreateMeetingRequest
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime? ScheduledAt { get; set; }
    }
}
