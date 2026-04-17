using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IProjectRepository
{
    public interface IProjectRepository
    {
        Task AddAsync(Project project);
        Task<Project?> GetByIdWithMembersAsync(Guid id);
        //Task<Project?> GetByIdAsync(Guid id);
        Task SaveChangesAsync();


    }
}
