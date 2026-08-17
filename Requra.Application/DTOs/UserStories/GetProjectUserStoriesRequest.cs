using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.UserStories
{
    public class GetProjectUserStoriesRequest
    {
        public Guid ProjectId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public List<string>? Status { get; set; }
        public string? Search { get; set; }
    }
}
