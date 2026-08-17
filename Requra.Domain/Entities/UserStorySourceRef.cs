using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class UserStorySourceRef
    {
        public Guid Id { get; private set; }

        public int? Page { get; private set; } //so far only as I am not sure it returns int or not from AI

        public string? Quote { get; private set; }

        public string? ChunkId { get; private set; }

        public string? SourceId { get; private set; }

        public string? SourceType { get; private set; }

        public string? DocumentName { get; private set; }

        public double ConfidenceScore { get; private set; }

        public Guid UserStoryId { get; private set; }

        public UserStory UserStory { get; private set; } = null!;

        // EF Core parameterless constructor
        private UserStorySourceRef()
        {
        }

        public UserStorySourceRef(
       int? page,
       string? quote,
       string? chunkId,
       string? sourceId,
       string? sourceType,
       string? documentName,
       double confidenceScore)
        {
            Id = Guid.NewGuid();

            Page = page;
            Quote = quote;
            ChunkId = chunkId;
            SourceId = sourceId;
            SourceType = sourceType;
            DocumentName = documentName;
            ConfidenceScore = confidenceScore;
        }
    }
}
