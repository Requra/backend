using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class RequirementSourceReference
    {
        public Guid Id { get; private set; }

        public Guid RequirementId { get; private set; }

        public int? Page { get; private set; }

        public string? Quote { get; private set; }

        public string? ChunkId { get; private set; }

        public string? SourceId { get; private set; }

        public string? SourceType { get; private set; }

        public string? DocumentName { get; private set; }

        public double? ConfidenceScore { get; private set; }

        public Requirement Requirement { get; private set; } = null!;

        public Guid? DocumentId { get; private set; }

        public Document? Document { get; private set; }

        private RequirementSourceReference()
        {
        }

        public RequirementSourceReference(
            int? page,
            string? quote,
            string? chunkId,
            string? sourceId,
            string? sourceType,
            string? documentName,
            double? confidenceScore,
            Guid? documentId)
        {
            Id = Guid.NewGuid();

            Page = page;
            Quote = quote;
            ChunkId = chunkId;
            SourceId = sourceId;
            SourceType = sourceType;
            DocumentName = documentName;
            ConfidenceScore = confidenceScore;
            DocumentId = documentId;
        }
    }
}
