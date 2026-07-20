using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectReviewInvitaion
{
    public class CreateProjectReviewInvitationRequest
    {
        public List<string>? StakeholderIds { get; set; }
        public List<NewProjectReviewStakeholderInput>? Stakeholders { get; set; }

        public ProjectReviewPermission Permission { get; set; } = default!; // VIEWER / COMMENTER
        public DateTime? ExpiresAt { get; set; }
    }

    public class NewProjectReviewStakeholderInput
    {
        public string DisplayName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? RoleTitle { get; set; }
        public string? Company { get; set; }
    }
}
