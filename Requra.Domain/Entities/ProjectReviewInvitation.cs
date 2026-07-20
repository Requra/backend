using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class ProjectReviewInvitation
    {
        public Guid Id { get; set; }

        public string ProjectId { get; set; }
        public string StakeholderId { get; set; }

        public string Email { get; set; }
        public string DisplayName { get; set; }

        public ProjectReviewPermission Permission { get; set; }
        public string Status { get; set; } = "PENDING";

        public string ReviewToken { get; set; }
        public string ReviewUrl { get; set; }

        public DateTime? ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public string InvitedById { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
