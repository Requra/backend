using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class RecordingConfiguration : IEntityTypeConfiguration<Recording>
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

            builder.Property(r => r.UploadMode)
                .HasColumnName("upload_mode")
                .HasConversion<string>()
                .IsRequired();

            builder.Property(r => r.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(255);

            builder.Property(r => r.OriginalExtension)
                .HasColumnName("original_extension")
                .HasMaxLength(50);

            builder.Property(r => r.StorageUrl)
                .HasColumnName("storage_url")
                .HasMaxLength(2000);

            builder.Property(r => r.StorageKey)
                .HasColumnName("storage_key")
                .HasMaxLength(1000);

            builder.Property(r => r.PublicId)
                .HasColumnName("public_id")
                .HasMaxLength(500);

            builder.Property(r => r.ReceivedBytes)
                .HasColumnName("received_bytes")
                .IsRequired();

            builder.Property(r => r.FinalFileSizeBytes)
                .HasColumnName("final_file_size_bytes");

            builder.Property(r => r.UploadedChunks)
                .HasColumnName("uploaded_chunks")
                .IsRequired();

            builder.Property(r => r.ExpectedChunks)
                .HasColumnName("expected_chunks");

            builder.Property(r => r.LastChunkReceivedAt)
                .HasColumnName("last_chunk_received_at");

            builder.Property(r => r.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired();

            builder.Property(r => r.FailureReason)
                .HasColumnName("failure_reason")
                .HasMaxLength(1000);

            builder.Property(r => r.FinalizationError)
                .HasColumnName("finalization_error")
                .HasMaxLength(4000);

            builder.Property(r => r.StartedAt)
                .HasColumnName("started_at")
                .IsRequired();

            builder.Property(r => r.StoppedAt)
                .HasColumnName("stopped_at");

            builder.Property(r => r.CompletedAt)
                .HasColumnName("completed_at");

            builder.Property(r => r.AbandonedAt)
                .HasColumnName("abandoned_at");

            builder.Property(r => r.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            // PostgreSQL concurrency token
            builder.Property(r => r.xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

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

            builder.HasIndex(r => new { r.MeetingId, r.Status });

            builder.HasIndex(r => r.CreatedById);

            builder.HasIndex(r => r.MeetingId)
                .IsUnique()
                .HasFilter("\"status\" IN ('Started', 'Uploading', 'Ending')")
                .HasDatabaseName("ux_recordings_meeting_id_one_active");
        }
    }
}
