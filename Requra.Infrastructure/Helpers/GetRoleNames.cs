using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Helpers
{
    public static class RoleHelper
    {
        public static List<string> GetRoleNames(UserRole roles)
        {
            var roleNames = new List<string>();

            if (roles.HasFlag(UserRole.Stakeholder))
                roleNames.Add(nameof(UserRole.Stakeholder));

            if (roles.HasFlag(UserRole.BusinessAnalyst))
                roleNames.Add(nameof(UserRole.BusinessAnalyst));

            if (roles.HasFlag(UserRole.ProjectManager))
                roleNames.Add(nameof(UserRole.ProjectManager));

            return roleNames;
        }
    }
}
