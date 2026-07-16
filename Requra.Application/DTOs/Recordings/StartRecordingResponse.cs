using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class StartRecordingResponse
    {
        public Guid Id { get; set; }

        public Guid MeetingId { get; set; }

        public string FileUrl { get; set; } = null!;

        public string MimeType { get; set; }
        public RecordingUploadMode UploadMode { get; set; }

        public RecordingStatus Status { get; set; }

        public int? DurationSeconds { get; set; }
        public int ChunksCount { get; set; } = 0;
        public List<int> MissingChunkIndexes { get; set; } = new List<int>();
        public DateTime CreatedAt { get; set; }=DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public Guid? DocumentId { get; set; }

    }
}
