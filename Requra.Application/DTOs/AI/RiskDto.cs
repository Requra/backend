using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class RiskDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
    }
}
