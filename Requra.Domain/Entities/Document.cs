using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; private set; }

        public Guid ProjectId { get; private set; }

        public Guid UploadedById { get; private set; }

        public Guid? MeetingId { get; private set; }

        public string Title { get; private set; } = null!;

        public DocumentType Type { get; private set; }

        public Language Language { get; private set; }

        public string? StorageUrl { get; private set; }

        public long? FileSize { get; private set; }

        public string? TranscriptText { get; private set; }

        public DocumentStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        // Navigation
        public Project Project { get; private set; } = null!;
        public ApplicationUser Uploader { get; private set; } = null!;
        public MeetingSession? Meeting { get; private set; }

        public ICollection<DocumentModel> DocumentModels { get; private set; } = new List<DocumentModel>();
        public ICollection<DocumentRequirement> DocumentRequirements { get; private set; } = new List<DocumentRequirement>();
        public ICollection<Summary> Summaries { get; private set; } = new List<Summary>();

        // Constructor
        public Document(Guid projectId, Guid uploadedById, string title, DocumentType type, Language language)
        {
            Id = Guid.NewGuid();
            ProjectId = projectId;
            UploadedById = uploadedById;
            Title = title;
            Type = type;
            Language = language;

            Status = DocumentStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateDetails(string title, DocumentType type, Language language)
        {
            Title = title;
            Type = type;
            Language = language;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetStorage(string url, long? size)
        {
            StorageUrl = url;
            FileSize = size;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetTranscript(string text)
        {
            TranscriptText = text;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkProcessing()
        {
            Status = DocumentStatus.Processing;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkCompleted()
        {
            Status = DocumentStatus.Ready;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkFailed()
        {
            Status = DocumentStatus.Failed;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}