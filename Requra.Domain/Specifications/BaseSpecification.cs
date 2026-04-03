using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Requra.Domain.Specifications
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        public Expression<Func<T, bool>>? Criteria { get; protected set; }

        public List<Expression<Func<T, object>>> Includes { get; } = [];

        public Expression<Func<T, object>>? OrderBy { get; protected set; }
        public Expression<Func<T, object>>? OrderByDesc { get; protected set; }

        public int Skip { get; protected set; }
        public int Take { get; protected set; }
        public bool IsPagingEnabled { get; protected set; }

        protected void AddInclude(Expression<Func<T, object>> include)
            => Includes.Add(include);

        protected void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }

        protected void ApplyOrderBy(Expression<Func<T, object>> orderBy)
            => OrderBy = orderBy;

        protected void ApplyOrderByDesc(Expression<Func<T, object>> orderByDesc)
            => OrderByDesc = orderByDesc;
    }
}
