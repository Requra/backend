using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class SourceDocumentDto
    {
        public Guid Backend_Document_Id { get; set; }

        public string Title { get; set; }

        public DocumentType Type { get; set; } 

        public int Language { get; set; }
    }
}
