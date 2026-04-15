using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Enums
{
    [Flags]

    public enum ProjectType
    {
        None = 0,
        Financial = 1,
        Medical = 2,
        Educational = 4
      
    }
}
