using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class GetRecordingStatusResponse
    {
        public Guid Id { get; set; }
        public Guid MeetingId { get; set; }
        public RecordingStatus Status { get; set; }
        public RecordingUploadMode UploadMode { get; set; }
        public string? MimeType { get; set; }
        public string? FileUrl { get; set; }
        public int? DurationSeconds { get; set; }
        public int ChunksCount { get; set; }
        public List<int> MissingChunkIndexes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? DocumentId { get; set; }
    }
}
