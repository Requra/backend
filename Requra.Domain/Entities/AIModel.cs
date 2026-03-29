using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Requra.Domain.Entities
{
    public class AIModel
    {
        
        public Guid Id { get; private set; } 

        public JobType JobType { get; private set; }

        public string ModelName { get; private set; } = null!;

        public string? Version { get; private set; }

        public Language? Language { get; private set; }

        public AIModelStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime? CompletedAt { get; private set; }

        public string? ErrorMessage { get; private set; }

        // Navigation
        public ICollection<DocumentModel> DocumentModels { get; private set; } = new List<DocumentModel>();
        public ICollection<Summary> Summaries { get; private set; } = new List<Summary>();

        private AIModel()
        {
            
        }
        public AIModel(string modelName, JobType jobType, Language? language = null)
        {
            Id = Guid.NewGuid();
            ModelName = modelName;
            JobType = jobType;
            Language = language;
            Status = AIModelStatus.Queued;
            CreatedAt = DateTime.UtcNow;
        }

        
        public void MarkCompleted()
        {
            Status = AIModelStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        public void MarkFailed(string error)
        {
            Status = AIModelStatus.Failed;
            ErrorMessage = error;
            CompletedAt = DateTime.UtcNow;
        }
    }
}
