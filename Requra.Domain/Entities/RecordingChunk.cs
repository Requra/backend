using Requra.Domain.Enums;
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

        public string? StorageKey { get; private set; }

        public string? PublicId { get; private set; }

        public long Size { get; private set; }

        public string? Checksum { get; private set; }

        public string? ContentType { get; private set; }

        public RecordingChunkStatus Status { get; private set; }

        public int UploadAttemptCount { get; private set; }
        public long? EndedAtMs { get; private set; }
        public long? StartedAtMs { get; private set; }

        public DateTime ReceivedAt { get; private set; }

        public DateTime UploadedAt { get; private set; }

        public string? ErrorMessage { get; private set; }
        //public byte[] RowVersion { get; private set; } = null!;
        public uint xmin { get; private set; }

        // Navigation
        public Recording Recording { get; private set; } = null!;

        private RecordingChunk()
        {
        }

        public RecordingChunk(Guid recordingId,int chunkNumber,string storageUrl,long size,string? storageKey = null,string? publicId = null,string? checksum = null,string? contentType = null, long? startedAtMs = null, long? endedAtMs = null)
        {
            Id = Guid.NewGuid();
            RecordingId = recordingId;
            ChunkNumber = chunkNumber;
            StorageUrl = storageUrl;
            StorageKey = storageKey;
            PublicId = publicId;
            Size = size;
            Checksum = checksum;
            ContentType = contentType;
            Status = RecordingChunkStatus.Uploaded;
            UploadAttemptCount = 1;
            ReceivedAt = DateTime.UtcNow;
            UploadedAt = DateTime.UtcNow;
            StartedAtMs = startedAtMs;
            EndedAtMs = endedAtMs;
        }

        public void MarkDuplicate()
        {
            Status = RecordingChunkStatus.Duplicate;
        }

        public void MarkFailed(string errorMessage)
        {
            Status = RecordingChunkStatus.Failed;
            ErrorMessage = errorMessage;
        }

        public void IncrementAttempt()
        {
            UploadAttemptCount++;
        }
        public void SetTimeRange(long? startedAtMs, long? endedAtMs)
        {
            StartedAtMs = startedAtMs;
            EndedAtMs = endedAtMs;
        }
    }
}
