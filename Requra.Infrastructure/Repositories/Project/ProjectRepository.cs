using Microsoft.EntityFrameworkCore;
using Requra.Application.Interfaces.IProjectRepository;
using Requra.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Repositories.Project
{
    public class ProjectRepository(RequraDbContext _context) : IProjectRepository
    {
        public async Task AddAsync(Domain.Entities.Project project)
        {
            await _context.Projects.AddAsync(project);

            await _context.SaveChangesAsync();
        }

        public async Task<Domain.Entities.Project?> GetByIdWithMembersAsync(Guid id)
        {
            return await _context.Projects
                .Include(p => p.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
