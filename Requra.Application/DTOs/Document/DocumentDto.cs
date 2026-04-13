using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Document
{
    public class DocumentDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;

        public string Type { get; set; } = null!;

        public string Status { get; set; } = null!;

        public string Language { get; set; } = null!;

        public string? StorageUrl { get; set; }

        public long? FileSize { get; set; }

        public string? UploadedBy { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
