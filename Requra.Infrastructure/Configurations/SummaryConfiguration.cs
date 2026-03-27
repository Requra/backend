using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class SummaryConfiguration : IEntityTypeConfiguration<Summary>
    {
        public void Configure(EntityTypeBuilder<Summary> builder)
        {
            builder.ToTable("summaries");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(s => s.DocumentId)
                   .HasColumnName("document_id")
                   .IsRequired();

            builder.Property(s => s.AIModelId)
                   .HasColumnName("ai_model_id")
                   .IsRequired();

            builder.Property(s => s.ExecutiveSummary)
                   .HasColumnName("executive_summary")
                   .HasColumnType("text");

            builder.Property(s => s.KeyDecisions)
                   .HasColumnName("key_decisions")
                   .HasColumnType("text");

            builder.Property(s => s.OpenQuestions)
                   .HasColumnName("open_questions")
                   .HasColumnType("text");

            builder.Property(s => s.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(s => s.Language)
                   .HasColumnName("language")
                   .HasConversion<string>();

            builder.Property(s => s.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            builder.Property(s => s.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            

            builder.HasOne(s => s.Document)
                   .WithMany(d => d.Summaries)
                   .HasForeignKey(s => s.DocumentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.AIModel)
                   .WithMany(m => m.Summaries)
                   .HasForeignKey(s => s.AIModelId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
