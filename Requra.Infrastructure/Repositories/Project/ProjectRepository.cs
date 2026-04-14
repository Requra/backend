using Microsoft.EntityFrameworkCore;
using Requra.Application.Interfaces.IProjectRepository;
using Requra.Infrastructure.Data;
using Requra.Domain.Entities; 
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Repositories.Project
{
    public class ProjectRepository(RequraDbContext _context) : IProjectRepository
    {
        public async Task AddAsync(Project project)
        {
            
        }

        public Task AddAsync(Domain.Entities.Project project)
        {
            throw new NotImplementedException();
        }
    }
}
