using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class Summary
    {
        public Guid Id { get; private set; }

        public Guid DocumentId { get; private set; }

        public Guid AIModelId { get; private set; }

        public string? ExecutiveSummary { get; private set; }

        public string? KeyDecisions { get; private set; }

        public string? OpenQuestions { get; private set; }

        public SummaryStatus Status { get; private set; }

        public Language? Language { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        // Navigation
        public Document Document { get; private set; } = null!;
        public AIModel AIModel { get; private set; } = null!;

        // Constructor
        public Summary(Guid documentId, Guid aiModelId, Language? language = null)
        {
            Id = Guid.NewGuid();
            DocumentId = documentId;
            AIModelId = aiModelId;
            Language = language;

            Status = SummaryStatus.Draft;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

       

        public void SetContent(string? executive, string? decisions, string? questions)
        {
            ExecutiveSummary = executive;
            KeyDecisions = decisions;
            OpenQuestions = questions;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkProcessing()
        {
            Status = SummaryStatus.Processing;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkCompleted()
        {
            Status = SummaryStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkFailed()
        {
            Status = SummaryStatus.Failed;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}