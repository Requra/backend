using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectMembers
{
    public class GetProjectMembersQuery
    {
        public string? Search { get; set; }
        public int? PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
