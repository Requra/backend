using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Profile
{
    public class UploadAvatarDto
    {
        public IFormFile File { get; set; }
    }
}
