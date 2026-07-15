using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class StartRecordingRequest
    {
        public Guid MeetingId { get; set; }

        public string CreatedById { get; set; } = null!;

        public string FileName { get; set; } = null!;

        public RecordingUploadMode UploadMode { get; set; }

        public string? ContentType { get; set; }

        public string? OriginalExtension { get; set; }

        public int? ExpectedChunks { get; set; }
    }
}
