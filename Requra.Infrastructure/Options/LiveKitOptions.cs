using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Options
{
    public class LiveKitOptions
    {
        public const string SectionName = "LiveKit";

        public string Url { get; set; } = null!;        
        public string ApiKey { get; set; } = null!;
        public string ApiSecret { get; set; } = null!;
    }
}
