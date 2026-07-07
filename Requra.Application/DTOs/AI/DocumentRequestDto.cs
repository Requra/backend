using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class DocumentRequestDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = default!;
        public string FileUrl { get; set; } = default!;
        public string? ContentType { get; set; }
    }
}
