using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Options
{
    public class MeetingInvitationLinkOptions
    {
        public string WebBaseUrl { get; set; } = string.Empty;
        public string MobileAppLinkBaseUrl { get; set; } = string.Empty;
    }
}
