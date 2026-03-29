using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class Approval
    {
        public Guid Id { get; private set; }

        public Guid RequirementId { get; private set; }

        public string ReviewerId { get; private set; } = null!;

        public ApprovalDecision Decision { get; private set; }

        public string? Notes { get; private set; }

        public DateTime? ReviewedAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        // Navigation
        public Requirement Requirement { get; private set; } = null!;
        public ApplicationUser Reviewer { get; private set; } = null!;

        // Constructor
        private Approval()
        {
            
        }
        public Approval(Guid requirementId, string reviewerId)
        {
            Id = Guid.NewGuid();
            RequirementId = requirementId;
            ReviewerId = reviewerId;

            Decision = ApprovalDecision.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void Approve(string? notes = null)
        {
            Decision = ApprovalDecision.Approved;
            Notes = notes;
            ReviewedAt = DateTime.UtcNow;
        }

        public void Reject(string? notes = null)
        {
            Decision = ApprovalDecision.Rejected;
            Notes = notes;
            ReviewedAt = DateTime.UtcNow;
        }
    }
}