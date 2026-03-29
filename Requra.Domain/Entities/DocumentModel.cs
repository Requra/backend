namespace Requra.Domain.Entities
{
    public class DocumentModel
    {
        public Guid DocumentId { get; private set; }
        public Guid AIModelId { get; private set; }

        private DocumentModel() { }

        public DocumentModel(Guid documentId, Guid aiModelId)
        {
            DocumentId = documentId;
            AIModelId = aiModelId;
        }

        // Navigation
        public Document Document { get; private set; } = null!;
        public AIModel AIModel { get; private set; } = null!;
    }
}