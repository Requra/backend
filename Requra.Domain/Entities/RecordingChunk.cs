using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class RecordingChunk
    {
        public Guid Id { get; private set; }

        public Guid RecordingId { get; private set; }

        public int ChunkNumber { get; private set; }

        public string StorageUrl { get; private set; } = null!;
        public string? PublicId { get; private set; }

        public long Size { get; private set; }

        public DateTime UploadedAt { get; private set; }

        // Navigation
        public Recording Recording { get; private set; } = null!;

        private RecordingChunk()
        {
        }

        public RecordingChunk(
            Guid recordingId,
            int chunkNumber,
            string storageUrl,
            long size)
        {
            Id = Guid.NewGuid();
            RecordingId = recordingId;
            ChunkNumber = chunkNumber;
            StorageUrl = storageUrl;
            Size = size;
            UploadedAt = DateTime.UtcNow;
        }
    }
}
