using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class StopRecordingResponse
    {
        public Guid RecordingId { get; set; }
        public RecordingStatus Status { get; set; }
        public int UploadedChunks { get; set; }
        public int? ExpectedChunks { get; set; }
        public List<int> MissingChunks { get; set; } = new();
        public DateTime? StoppedAt { get; set; }
        public string Message { get; set; } = null!;
    }
}
