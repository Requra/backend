namespace Requra.Domain.Entities
{
    public class DocumentRequirement
    {
        public Guid DocumentId { get; private set; }

        public Guid RequirementId { get; private set; }

        // Navigation
        public Document Document { get; private set; } = null!;
        public Requirement Requirement { get; private set; } = null!;

        public DocumentRequirement(Guid documentId, Guid requirementId)
        {
            DocumentId = documentId;
            RequirementId = requirementId;
        }
    }
}