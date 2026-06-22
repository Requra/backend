using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class SummaryDto
    {
        public string Overview { get; set; } = string.Empty;
        public List<string> KeyPoints { get; set; } = new();
    }
}
