using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
        public class AnalysisResultConfiguration : IEntityTypeConfiguration<AnalysisResult>
        {
            public void Configure(EntityTypeBuilder<AnalysisResult> builder)
            {
                builder.ToTable("analysis_results");

                builder.HasKey(x => x.Id);

                builder.Property(x => x.Id)
                       .HasColumnName("id")
                       .HasDefaultValueSql("gen_random_uuid()");

                builder.Property(x => x.AnalysisRunId)
                       .HasColumnName("analysis_run_id")
                       .IsRequired();

                builder.Property(x => x.RawJson)
                       .HasColumnName("raw_json")
                       .HasColumnType("jsonb")   
                       .IsRequired();

                builder.Property(x => x.CreatedAt)
                       .HasColumnName("created_at")
                       .HasColumnType("timestamptz")
                       .HasDefaultValueSql("NOW()");

                // One-to-one relation (Run → Result)
                builder.HasOne<AnalysisRun>()
                       .WithOne()
                       .HasForeignKey<AnalysisResult>(x => x.AnalysisRunId)
                       .OnDelete(DeleteBehavior.Cascade);
            }
        }
    
}
