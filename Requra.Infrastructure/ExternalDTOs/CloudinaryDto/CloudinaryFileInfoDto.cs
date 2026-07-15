using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalDTOs.CloudinaryDto
{
    public class CloudinaryFileInfoDto
    {
        public string PublicId { get; set; } = null!;
        public string ResourceType { get; set; } = null!;
        public string? Format { get; set; }
        public long? Bytes { get; set; }
        public string? SecureUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
