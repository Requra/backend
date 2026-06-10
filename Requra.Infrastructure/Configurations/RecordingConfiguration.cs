using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class RecordingConfiguration:IEntityTypeConfiguration<Recording>
    {
        public void Configure(EntityTypeBuilder<Recording> builder)
        {
            builder.ToTable("recordings");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .HasColumnName("id");

            builder.Property(r => r.MeetingId)
                .HasColumnName("meeting_id")
                .IsRequired();

            builder.Property(r => r.CreatedById)
                .HasColumnName("created_by_id")
                .IsRequired();

            builder.Property(r => r.FileName)
                .HasColumnName("file_name")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(r => r.StorageUrl)
                .HasColumnName("storage_url");

            builder.Property(r => r.PublicId)
                .HasColumnName("public_id");

            builder.Property(r => r.TotalSizeBytes)
                .HasColumnName("total_size_bytes");

            builder.Property(r => r.UploadedChunks)
                .HasColumnName("uploaded_chunks");

            builder.Property(r => r.ExpectedChunks)
                .HasColumnName("expected_chunks");

            builder.Property(r => r.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired();

            builder.Property(r => r.StartedAt)
                .HasColumnName("started_at");

            builder.Property(r => r.CompletedAt)
                .HasColumnName("completed_at");

            builder.Property(r => r.CreatedAt)
                .HasColumnName("created_at");

            builder.Property(r => r.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasOne(r => r.Meeting)
                .WithMany(m => m.Recordings)
                .HasForeignKey(r => r.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.MeetingId);

            builder.HasIndex(r => r.Status);
        }
    }
}
