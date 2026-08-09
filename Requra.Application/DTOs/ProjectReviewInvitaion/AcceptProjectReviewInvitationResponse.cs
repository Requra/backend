using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectReviewInvitaion
{
    public class AcceptProjectReviewInvitationResponse
    {
        public Guid ProjectId { get; set; } 
        public string AccessId { get; set; } = null!;
        public ProjectReviewPermission Permission { get; set; }
        public InvitationStatus Status { get; set; }
        public DateTime? AcceptedAt { get; set; }
    }
}
