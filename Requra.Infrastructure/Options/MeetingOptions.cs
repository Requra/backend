using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Options
{
    public class MeetingOptions
    {
        public const string SectionName = "Meeting";

        public int MvpMaxLiveDurationMinutes { get; set; } = 60;
    }
}
