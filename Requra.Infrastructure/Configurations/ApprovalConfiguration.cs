using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
    {
        public void Configure(EntityTypeBuilder<Approval> builder)
        {
            builder.ToTable("approvals");

            // PK
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(a => a.RequirementId)
                   .HasColumnName("requirement_id")
                   .IsRequired();

            builder.Property(a => a.ReviewerId)
                   .HasColumnName("reviewer_id")
                   .IsRequired();

            builder.Property(a => a.Decision)
                   .HasColumnName("decision")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(a => a.Notes)
                   .HasColumnName("notes")
                   .HasColumnType("text");

            builder.Property(a => a.ReviewedAt)
                   .HasColumnName("reviewed_at")
                   .HasColumnType("timestamptz");

            builder.Property(a => a.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

           

            builder.HasOne(a => a.Requirement)
                   .WithMany(r => r.Approvals)
                   .HasForeignKey(a => a.RequirementId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Reviewer)
                   .WithMany(u => u.Approvals)
                   .HasForeignKey(a => a.ReviewerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
