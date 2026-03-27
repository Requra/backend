using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class DocumentRequirementConfiguration : IEntityTypeConfiguration<DocumentRequirement>
    {
        public void Configure(EntityTypeBuilder<DocumentRequirement> builder)
        {
            builder.ToTable("document_requirements");

           
            builder.HasKey(dr => new { dr.DocumentId, dr.RequirementId });

            builder.Property(dr => dr.DocumentId)
                   .HasColumnName("document_id")
                   .IsRequired();

            builder.Property(dr => dr.RequirementId)
                   .HasColumnName("requirement_id")
                   .IsRequired();

            // Relationships

            builder.HasOne(dr => dr.Document)
                   .WithMany(d => d.DocumentRequirements)
                   .HasForeignKey(dr => dr.DocumentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(dr => dr.Requirement)
                   .WithMany(r => r.DocumentRequirements)
                   .HasForeignKey(dr => dr.RequirementId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
