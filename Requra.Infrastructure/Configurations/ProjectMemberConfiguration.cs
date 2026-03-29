using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> builder)
        {
            builder.ToTable("project_members");

            builder.HasKey(pm => new { pm.UserId, pm.ProjectId });

            builder.Property(pm => pm.UserId)
                   .HasColumnName("user_id")
                   .IsRequired();

            builder.Property(pm => pm.ProjectId)
                   .HasColumnName("project_id")
                   .IsRequired();

            builder.Property(pm => pm.Role)
                   .HasColumnName("role")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(pm => pm.JoinedAt)
                   .HasColumnName("joined_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            // Relationships
            builder.HasOne(pm => pm.User)
                   .WithMany(u=>u.ProjectMemberships) 
                   .HasForeignKey(pm => pm.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pm => pm.Project)
                   .WithMany() 
                   .HasForeignKey(pm => pm.ProjectId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
