using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
        public class AnalysisRunConfiguration : IEntityTypeConfiguration<AnalysisRun>
        {
            public void Configure(EntityTypeBuilder<AnalysisRun> builder)
            {
                builder.ToTable("analysis_runs");

                builder.HasKey(x => x.Id);

                builder.Property(x => x.Id)
                       .HasColumnName("id")
                       .HasDefaultValueSql("gen_random_uuid()");

                builder.Property(x => x.ProjectId)
                       .HasColumnName("project_id")
                       .IsRequired();

                builder.Property(x => x.Status)
                       .HasColumnName("status")
                       .HasConversion<string>()
                       .HasMaxLength(50)
                       .IsRequired();

                builder.Property(x => x.Progress)
                       .HasColumnName("progress")
                       .HasDefaultValue(0);

                builder.Property(x => x.ErrorMessage)
                       .HasColumnName("error_message")
                       .HasColumnType("text");

                builder.Property(x => x.CreatedAt)
                       .HasColumnName("created_at")
                       .HasColumnType("timestamptz")
                       .HasDefaultValueSql("NOW()");

                builder.Property(x => x.StartedAt)
                       .HasColumnName("started_at")
                       .HasColumnType("timestamptz");

                builder.Property(x => x.CompletedAt)
                       .HasColumnName("completed_at")
                       .HasColumnType("timestamptz");

                //// Optional: relation to Project (if you have navigation)
                //builder.HasOne<Project>()
                //       .WithMany()
                //       .HasForeignKey(x => x.ProjectId)
                //       .OnDelete(DeleteBehavior.Cascade);
            }
        
    }
}
