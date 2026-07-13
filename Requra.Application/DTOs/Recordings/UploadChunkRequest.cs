using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class UploadChunkRequest
    {
        public Guid RecordingId { get; set; }
        public int ChunkNumber { get; set; }
        public Stream ChunkStream { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long Size { get; set; }
        public string? Checksum { get; set; }
    }
}
