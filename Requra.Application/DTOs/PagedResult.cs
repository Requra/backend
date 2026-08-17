using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = [];
        public int TotalCount { get; set; } 

        public int? PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 25;
        public int? TotalPages { get; set; }
     }
}
