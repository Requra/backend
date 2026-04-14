using Microsoft.AspNetCore.Http;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Document
{
    public class UploadDocumentDto
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = null!;
        public DocumentType Type { get; set; }
        public Language Language { get; set; }
        public Guid? MeetingId { get; set; }
        public IFormFile File { get; set; } = null!;
    }
}
