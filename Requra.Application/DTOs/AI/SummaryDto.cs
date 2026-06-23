using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class SummaryDto
    {
        public string ExecutiveSummary { get; set; }
        public string Scope { get; set; }
        public List<string> MainActors { get; set; }
        public List<string> MainGoals { get; set; }
    }
}
