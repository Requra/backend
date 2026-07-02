using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class AnalysisResult
    {
        public Guid Id { get; set; }
        public Guid AnalysisRunId { get; set; }

        public string RawJson { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
