using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Data
{
    public class RequraDbContext : IdentityDbContext<ApplicationUser>
    {
        public RequraDbContext(DbContextOptions<RequraDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            builder.ApplyConfigurationsFromAssembly(typeof(RequraDbContext).Assembly);

            base.OnModelCreating(builder);
        }

        public DbSet<ApplicationUser> ApplicationUser { get; set; }

    }
}
