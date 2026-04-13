using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalDTOs.CloudinaryDto
{
    public class UploadResultDto
    {
        public string PublicId { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string? ResourceType { get; set; } // image, video, raw
        public long? Size { get; set; }
    }
}
