using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class RequirementDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Priority { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> SourceDocumentIds { get; set; }
    }
}
