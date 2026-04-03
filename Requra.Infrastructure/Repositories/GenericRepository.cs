using Microsoft.EntityFrameworkCore;
using Requra.Domain.Specifications;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.Specification;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly RequraDbContext _context;

        public GenericRepository(RequraDbContext context)
        {
            _context = context;
        }

        public async Task<T?> GetByIdAsync(Guid id)
            => await _context.Set<T>().FindAsync(id);
        public virtual async Task<T> GetByIdAsync(string id)
             =>await _context.Set<T>().FindAsync(id);
        

        public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
        {
            var query = ApplySpecification(spec);
            return await query.ToListAsync();
        }

        public async Task<int> CountAsync(ISpecification<T> spec)
        {
            var query = ApplySpecification(spec);
            return await query.CountAsync();
        }

        public async Task AddAsync(T entity)
            => await _context.Set<T>().AddAsync(entity);

        public void Update(T entity)
            => _context.Set<T>().Update(entity);

        public void Delete(T entity)
            => _context.Set<T>().Remove(entity);

        private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            return SpecificationEvaluator<T>.GetQuery(_context.Set<T>().AsQueryable(), spec);
        }
    }
}
