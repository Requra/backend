using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class AnalysisRunDto
    {
        public Guid Id { get; set; }

        public Guid ProjectId { get; set; }

        public string Status { get; set; } = default!;

        public List<Guid> DocumentIds { get; set; } = new();

        public Guid? MeetingId { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
