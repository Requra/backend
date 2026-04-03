using Requra.Domain.Entities;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Specifications.ProjectsSpecification
{
    public class ProjectsByUserSpecification : BaseSpecification<Project>
    {
        public ProjectsByUserSpecification(
            string userId,
            ProjectStatus? status,
            int pageNumber,
            int pageSize)
        {
           
            Criteria = p =>
                p.Members.FirstOrDefault(p=>p.UserId==userId).UserId == userId &&
                (!status.HasValue || p.Status == status);

            ApplyOrderByDesc(p => p.CreatedAt);

            ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        }
    }
}
