using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class MetricsDto
    {
        public int TotalRequirements { get; set; }
        public int FunctionalRequirements { get; set; }
        public int NonFunctionalRequirements { get; set; }
        public int HighPriorityItems { get; set; }
        public int RisksCount { get; set; }
        public int OpenQuestionsCount { get; set; }
    }
}
