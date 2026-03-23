using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
    }
}
