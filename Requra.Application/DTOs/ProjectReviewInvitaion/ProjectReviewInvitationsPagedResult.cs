using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectReviewInvitaion
{
    public class ProjectReviewInvitationsPagedResult<T> : PagedResult<T>
    {
        public int PendingCount { get; set; }
        public int AcceptedCount { get; set; }
        public int RevokedCount { get; set; }
    }
}
