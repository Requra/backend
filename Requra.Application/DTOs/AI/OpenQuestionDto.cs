using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class OpenQuestionDto
    {
        public string Id { get; set; }
        public string Question { get; set; }
        public List<string> SourceDocumentIds { get; set; }
    }
}
