using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class FileUploadDto
    {
        public byte[] Content { get; set; }
        public string FileName { get; set; }
    }
}
