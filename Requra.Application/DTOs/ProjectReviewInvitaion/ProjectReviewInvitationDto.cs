using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectReviewInvitaion
{
    //public class ProjectReviewInvitationDto
    //{
    //    public Guid Id { get; set; }
    //    public string ProjectId { get; set; }
    //    public Guid StakeholderId { get; set; }

    //    public string Email { get; set; }
    //    public string DisplayName { get; set; }

    //    public string Permission { get; set; }
    //    public string Status { get; set; }

    //    public string ReviewUrl { get; set; }

    //    public DateTime? ExpiresAt { get; set; }
    //    public DateTime? AcceptedAt { get; set; }
    //    public DateTime? RevokedAt { get; set; }

    //    public string InvitedById { get; set; }

    //    public DateTime CreatedAt { get; set; }
    //    public DateTime UpdatedAt { get; set; }
    //}
    public class ProjectReviewInvitationDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string StakeholderId { get; set; }
        public string Email { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? Company { get; set; }
        public string? RoleTitle { get; set; }
        public ProjectReviewPermission Permission { get; set; } 
        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
        public string ReviewUrl { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string InvitedById { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
