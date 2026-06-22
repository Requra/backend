using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class ProcessJsonRequest
    {
        public string Job_Id { get; set; }

        public string Source_Type { get; set; }
        // e.g. "multi_document" or "meeting"

        public string Content { get; set; }

        public List<SourceDocumentDto> Source_Documents { get; set; } = new();

        public MetadataDto Metadata { get; set; }
    }
}
