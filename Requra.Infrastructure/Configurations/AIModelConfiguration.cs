using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class AIModelConfiguration : IEntityTypeConfiguration<AIModel>
    {
        public void Configure(EntityTypeBuilder<AIModel> builder)
        {
            builder.ToTable("ai_models");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.ModelName)
                   .HasColumnName("model_name")
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.JobType)
                   .HasColumnName("job_type")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(x => x.Version)
                   .HasColumnName("version")
                   .HasMaxLength(50);

            builder.Property(x => x.Language)
                   .HasColumnName("language")
                   .HasConversion<string>();

            builder.Property(x => x.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            builder.Property(x => x.CompletedAt)
                   .HasColumnName("completed_at")
                   .HasColumnType("timestamptz");

            builder.Property(x => x.ErrorMessage)
                   .HasColumnName("error_message")
                   .HasColumnType("text");

            // Relationships
            builder.HasMany(x => x.DocumentModels)
                   .WithOne(d => d.AIModel)
                   .HasForeignKey(d => d.AIModelId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Summaries)
                   .WithOne(s => s.AIModel)
                   .HasForeignKey(s => s.AIModelId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
