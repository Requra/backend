using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectReviewInvitaion
{
    public class RevokeInvitationResponseDto
    {
        public string Id { get; set; }
        public InvitationStatus Status { get; set; } = InvitationStatus.Revoked;
    }
}
