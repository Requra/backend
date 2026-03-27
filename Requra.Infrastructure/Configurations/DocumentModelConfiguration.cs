using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class DocumentModelConfiguration : IEntityTypeConfiguration<DocumentModel>
    {
        public void Configure(EntityTypeBuilder<DocumentModel> builder)
        {
            builder.ToTable("document_models");

            builder.HasKey(dm => new { dm.DocumentId, dm.AIModelId });

            builder.Property(dm => dm.DocumentId)
                   .HasColumnName("document_id")
                   .IsRequired();

            builder.Property(dm => dm.AIModelId)
                   .HasColumnName("ai_model_id")
                   .IsRequired();

            // Relationships

            builder.HasOne(dm => dm.Document)
                   .WithMany(d => d.DocumentModels)
                   .HasForeignKey(dm => dm.DocumentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(dm => dm.AIModel)
                   .WithMany(m => m.DocumentModels)
                   .HasForeignKey(dm => dm.AIModelId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
