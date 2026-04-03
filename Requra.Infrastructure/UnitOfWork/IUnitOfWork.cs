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
        Task<int> CompleteAsync();
    }
}
