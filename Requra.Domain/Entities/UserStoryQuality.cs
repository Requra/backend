using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class UserStoryQuality
    {
        public Guid Id { get; private set; }

        public double Score { get; private set; }

        public List<string> Issues { get; private set; } = new();

        public List<string> Warnings { get; private set; } = new();

        public QualityStatus QualityStatus { get; private set; } = QualityStatus.NOT_EVALUATED;

        public Guid UserStoryId { get; private set; }

        public UserStory UserStory { get; private set; } = null!;

        public UserStoryQuality(double score, List<string>? issues, List<string>? warnings)
        {
            Id = Guid.NewGuid();
            Score = score;
            Issues = issues ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            QualityStatus = QualityStatus.FRESH;
        }

        private UserStoryQuality()
        {
        }

        public void MarkStale()
        {
            QualityStatus = QualityStatus.STALE;
        }

        public void SetFresh(double score, List<string> issues, List<string> warnings)
        {
            Score = score;
            Issues = issues ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            QualityStatus = QualityStatus.FRESH;
        }
    }
}
