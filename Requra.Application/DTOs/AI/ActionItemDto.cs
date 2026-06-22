using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class ActionItemDto
    {
        public string Task { get; set; } = string.Empty;
        public string? Owner { get; set; }
    }
}
