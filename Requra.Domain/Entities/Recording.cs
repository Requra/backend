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
        public RecordingUploadMode UploadMode { get; private set; }
        public string? ContentType { get; private set; }
        public string? OriginalExtension { get; private set; }

        public string? StorageUrl { get; private set; }
        public string? StorageKey { get; private set; }
        public string? PublicId { get; private set; }

        public long ReceivedBytes { get; private set; }
        public long? FinalFileSizeBytes { get; private set; }

        public int UploadedChunks { get; private set; }
        public int? ExpectedChunks { get; private set; }

        public DateTime? LastChunkReceivedAt { get; private set; }

        public RecordingStatus Status { get; private set; }

        public string? FailureReason { get; private set; }
        public string? FinalizationError { get; private set; }

        public DateTime StartedAt { get; private set; }
        public DateTime? StoppedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public DateTime? AbandonedAt { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        //public byte[] RowVersion { get; private set; } = null!;
        public uint xmin { get; private set; }

        public MeetingSession Meeting { get; private set; } = null!;
        public ApplicationUser CreatedBy { get; private set; } = null!;
        public ICollection<RecordingChunk> Chunks { get; private set; } = new List<RecordingChunk>();

        private Recording() { }

        public Recording(Guid meetingId,string createdById,string fileName,RecordingUploadMode uploadMode,string? contentType = null,string? originalExtension = null,int? expectedChunks = null)
        {
            Id = Guid.NewGuid();
            MeetingId = meetingId;
            CreatedById = createdById;
            FileName = fileName;
            UploadMode = uploadMode;
            ContentType = contentType;
            OriginalExtension = originalExtension;
            ExpectedChunks = expectedChunks;

            Status = RecordingStatus.Started;
            StartedAt = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkUploading()
        {
            if (Status == RecordingStatus.Started)
            {
                Status = RecordingStatus.Uploading;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void RegisterChunk(long chunkSize)
        {
            if (chunkSize < 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));

            if (Status == RecordingStatus.Started)
                Status = RecordingStatus.Uploading;

            ReceivedBytes += chunkSize;
            UploadedChunks++;
            LastChunkReceivedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetExpectedChunks(int expectedChunks)
        {
            if (expectedChunks < 0)
                throw new ArgumentOutOfRangeException(nameof(expectedChunks));

            ExpectedChunks = expectedChunks;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetStorage(string storageUrl, string storageKey, string publicId, long finalFileSizeBytes)
        {
            StorageUrl = storageUrl;
            StorageKey = storageKey;
            PublicId = publicId;
            FinalFileSizeBytes = finalFileSizeBytes;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkFinalizing()
        {
            if (Status == RecordingStatus.Completed || Status == RecordingStatus.Failed || Status == RecordingStatus.Expired)
                throw new InvalidOperationException("Recording cannot be finalized from current state.");

            Status = RecordingStatus.Ending;
            StoppedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkCompleted()
        {
            Status = RecordingStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkFailed(string reason, string? finalizationError = null)
        {
            Status = RecordingStatus.Failed;
            FailureReason = reason;
            FinalizationError = finalizationError;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAbandoned(string? reason = null)
        {
            Status = RecordingStatus.Expired;
            FailureReason = reason;
            AbandonedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

    }
}

