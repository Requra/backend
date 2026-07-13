using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalDTOs.CloudinaryDto
{
    public class UploadResultDto
    {
        public bool IsSuccess { get; set; }
        public string? PublicId { get; set; }
        public string? Url { get; set; }
        public string? ResourceType { get; set; }
        public long Size { get; set; }
        public string? Format { get; set; }
        public string? OriginalFileName { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
