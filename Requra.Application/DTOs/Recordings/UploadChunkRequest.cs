using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    //public class UploadChunkRequest
    //{
    //    public Guid RecordingId { get; set; }
    //    public int ChunkNumber { get; set; }
    //    public Stream ChunkStream { get; set; } = null!;
    //    public string FileName { get; set; } = null!;
    //    public string ContentType { get; set; } = null!;
    //    public long Size { get; set; }
    //    public string? Checksum { get; set; }
    //}
    public class UploadChunkRequest
    {
        public Guid RecordingId { get; set; }
        public int? ChunkIndex { get; set; }
        public IFormFile AudioChunk { get; set; } = null!;
        public long? StartedAtMs { get; set; }
        public long? EndedAtMs { get; set; }
        public string? Checksum { get; set; }
    }
}
