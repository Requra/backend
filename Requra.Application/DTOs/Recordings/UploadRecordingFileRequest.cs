using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class UploadRecordingFileRequest
    {
        public Guid RecordingId { get; set; }
        public IFormFile File { get; set; } = null!;
    }
}
