using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Enums
{
    [Flags]
    public enum UserRole
    {
        None = 0,
        Stakeholder = 1,
        BusinessAnalyst = 2,
        ProjectManager = 4,

    }
    
}
