using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Seeder
{
     public static class RoleSeeder
    {
        private static readonly string[] Roles = ["ProjectManager", "BusinessAnalyst", "Stakeholder"];

        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
