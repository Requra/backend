using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class StartRecordingApiRequest
    {
        public RecordingUploadMode UploadMode { get; set; }
        public string MimeType { get; set; }
       
    }
}
