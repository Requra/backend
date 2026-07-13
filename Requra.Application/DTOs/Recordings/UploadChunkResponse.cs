using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class UploadChunkResponse
    {
        public Guid RecordingId { get; set; }
        public Guid ChunkId { get; set; }
        public int ChunkNumber { get; set; }
        public RecordingChunkStatus ChunkStatus { get; set; }
        public RecordingStatus RecordingStatus { get; set; }
        public bool IsDuplicate { get; set; }
        public int UploadedChunks { get; set; }
        public int? ExpectedChunks { get; set; }
        public long ReceivedBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
