using Requra.Domain.Entities;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Specifications.ProjectsSpecification
{
    public class ProjectsCountSpecification : BaseSpecification<Project>
    {
        public ProjectsCountSpecification(string userId, ProjectStatus? status)
        {
            Criteria = p =>
                p.Members.FirstOrDefault(p => p.UserId == userId).UserId == userId &&
                (!status.HasValue || p.Status == status);
        }
    }
}
