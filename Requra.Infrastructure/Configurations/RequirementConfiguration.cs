using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    using global::Requra.Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    namespace Requra.Infrastructure.Configurations
    {
        public class RequirementConfiguration : IEntityTypeConfiguration<Requirement>
        {
            public void Configure(EntityTypeBuilder<Requirement> builder)
            {
                builder.ToTable("requirements");

                builder.HasKey(r => r.Id);

                builder.Property(r => r.Id)
                       .HasColumnName("id")
                       .HasDefaultValueSql("gen_random_uuid()");

                builder.Property(r => r.Title)
                       .HasColumnName("title")
                       .HasMaxLength(255)
                       .IsRequired();

                builder.Property(r => r.Description)
                       .HasColumnName("description")
                       .HasColumnType("text");

                builder.Property(r => r.Type)
                       .HasColumnName("type")
                       .HasConversion<string>()
                       .IsRequired();

                builder.Property(r => r.Status)
                       .HasColumnName("status")
                       .HasConversion<string>()
                       .IsRequired();

                builder.Property(r => r.Language)
                       .HasColumnName("language")
                       .HasConversion<string>();

                builder.Property(r => r.CreatedAt)
                       .HasColumnName("created_at")
                       .HasColumnType("timestamptz")
                       .HasDefaultValueSql("NOW()");

                builder.Property(r => r.UpdatedAt)
                       .HasColumnName("updated_at")
                       .HasColumnType("timestamptz")
                       .HasDefaultValueSql("NOW()");

                // 🔥 Relationships

                builder.HasMany(r => r.DocumentRequirements)
                       .WithOne(dr => dr.Requirement)
                       .HasForeignKey(dr => dr.RequirementId)
                       .OnDelete(DeleteBehavior.Cascade);

                builder.HasMany(r => r.UserStories)
                       .WithOne(us => us.Requirement)
                       .HasForeignKey(us => us.RequirementId)
                       .OnDelete(DeleteBehavior.Cascade);

                builder.HasMany(r => r.Approvals)
                       .WithOne(a => a.Requirement)
                       .HasForeignKey(a => a.RequirementId)
                       .OnDelete(DeleteBehavior.Cascade);
            }
        }
    }
}
