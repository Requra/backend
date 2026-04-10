using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Requra.Infrastructure.Seeder;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Initializers
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            await RoleSeeder.SeedAsync(roleManager);
        }
    }
}
