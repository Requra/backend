using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class StartRecordingResponse
    {
        public Guid RecordingId { get; set; }

        public Guid MeetingId { get; set; }

        public string FileName { get; set; } = null!;

        public RecordingUploadMode UploadMode { get; set; }

        public RecordingStatus Status { get; set; }

        public DateTime StartedAt { get; set; }
    }
}
