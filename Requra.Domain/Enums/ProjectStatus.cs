using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Enums
{
    [Flags]
    public enum ProjectStatus
    {
        None = 0,
        InProgress = 1,
        Drafted = 2,
        Completed = 4,
        Cancelled = 8
    }
}
