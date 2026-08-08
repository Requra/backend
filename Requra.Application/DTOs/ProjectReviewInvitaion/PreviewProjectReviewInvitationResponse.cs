using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectReviewInvitaion
{
    public class PreviewProjectReviewInvitationResponse
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string StakeholderEmail { get; set; } = null!;
        public string? StakeholderDisplayName { get; set; }
        public ProjectReviewPermission Permission { get; set; }
        public InvitationStatus Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
    }
}
