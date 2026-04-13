using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalDTOs.CloudinaryDto
{
    public class CloudinarySettings
    {
        public string CloudName { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string ApiSecret { get; set; } = null!;
    }
}
