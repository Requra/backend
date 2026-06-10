using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class Recording
    {
        public Guid Id { get; private set; }

        public Guid MeetingId { get; private set; }

        public string CreatedById { get; private set; } = null!;

        public string FileName { get; private set; } = null!;

        public string? StorageUrl { get; private set; }

        public string? PublicId { get; private set; }

        public long TotalSizeBytes { get; private set; }

        public int UploadedChunks { get; private set; }

        public int ExpectedChunks { get; private set; }

        public RecordingStatus Status { get; private set; }

        public DateTime StartedAt { get; private set; }

        public DateTime? CompletedAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        // Navigation
        public MeetingSession Meeting { get; private set; } = null!;

        public ApplicationUser CreatedBy { get; private set; } = null!;
        public ICollection<RecordingChunk> Chunks { get; private set; }= new List<RecordingChunk>();

        private Recording()
        {
        }

        public Recording(
            Guid meetingId,
            string createdById,
            string fileName,
            int expectedChunks = 0)
        {
            Id = Guid.NewGuid();
            MeetingId = meetingId;
            CreatedById = createdById;
            FileName = fileName;
            ExpectedChunks = expectedChunks;

            Status = RecordingStatus.Recording;

            StartedAt = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void IncrementChunk(long chunkSize)
        {
            UploadedChunks++;
            TotalSizeBytes += chunkSize;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetStorage(string url, string publicId, long size)
        {
            StorageUrl = url;
            PublicId = publicId;
            TotalSizeBytes = size;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkCompleted()
        {
            Status = RecordingStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkFailed()
        {
            Status = RecordingStatus.Failed;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
