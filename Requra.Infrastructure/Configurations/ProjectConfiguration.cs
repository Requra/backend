using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("projects");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");


            builder.Property(p => p.Name)
                   .HasColumnName("name")
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(p => p.Description)
                   .HasColumnName("description")
                   .HasColumnType("text");

            builder.Property(p => p.Language)
                   .HasColumnName("language")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(p => p.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(p => p.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            builder.Property(p => p.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");


            builder.HasMany(p => p.Documents)
                   .WithOne(d => d.Project)
                   .HasForeignKey(d => d.ProjectId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Members)
                   .WithOne(m => m.Project)
                   .HasForeignKey(m => m.ProjectId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
