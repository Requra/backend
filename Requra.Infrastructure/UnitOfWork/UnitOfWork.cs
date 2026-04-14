using Microsoft.EntityFrameworkCore;
using Requra.Domain.Entities;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly RequraDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();

        public IGenericRepository<Project> Projects { get; private set; }
        public IGenericRepository<ApplicationUser> Users { get; private set; }
        public IGenericRepository<Document> Documents { get; private set; }
        public IGenericRepository<UserStory> UserStories { get; private set; }

        public UnitOfWork(RequraDbContext context)
        {
            _context = context;
            Projects = new GenericRepository<Project>(context);
            Users = new GenericRepository<ApplicationUser>(context);
            Documents = new GenericRepository<Document>(context);
            UserStories = new GenericRepository<UserStory>(context);
        }
        
        public IGenericRepository<T> Repository<T>() where T : class
        {
            if (_repositories.ContainsKey(typeof(T)))
                return (IGenericRepository<T>)_repositories[typeof(T)];

            var repo = new GenericRepository<T>(_context);
            _repositories.Add(typeof(T), repo);

            return repo;
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();

        }
        public void Dispose()
            => _context.Dispose();
    }
}
