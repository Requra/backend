using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class UploadRecordingFileResponse
    {
        public Guid RecordingId { get; set; }
        public string StorageUrl { get; set; } = null!;
        public string PublicId { get; set; } = null!;
        public long FinalFileSizeBytes { get; set; }
        public RecordingStatus Status { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
