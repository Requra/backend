using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectReviewInvitaion
{
    public class AcceptProjectReviewInvitationRequest
    {
        public string Token { get; set; } = null!;
        public string? DisplayName { get; set; }
    }

    public class AcceptProjectReviewInvitationAPIRequest
    {
        //public string Token { get; set; } = null!;
        public string? DisplayName { get; set; }
    }
}
