using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project
{
    public class ProjectFilter
    {
        public string UserId { get; set; }
        public ProjectStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class ProjectFilterAPI
    {
        //public string UserId { get; set; }
        public ProjectStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
