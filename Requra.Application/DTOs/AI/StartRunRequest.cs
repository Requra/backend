using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class StartRunRequest
    {
        public List<Guid>? DocumentIds { get; set; }
        public Guid? MeetingId { get; set; }
        public string AnalysisType { get; set; }
        public Language Language { get; set; }
    }
}
