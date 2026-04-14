using Microsoft.EntityFrameworkCore;
using Requra.Domain.Entities;
using Requra.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> Repository<T>() where T : class;
        IGenericRepository<Project> Projects { get; }
        IGenericRepository<ApplicationUser> Users { get; }
        IGenericRepository<Document> Documents { get; }
        IGenericRepository<UserStory> UserStories { get; }

        Task<int> SaveAsync();
    }
}
