using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class RecordingChunkConfiguration : IEntityTypeConfiguration<RecordingChunk>
    {
        public void Configure(EntityTypeBuilder<RecordingChunk> builder)
        {
            builder.ToTable("recording_chunks");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.RecordingId)
                .HasColumnName("recording_id")
                .IsRequired();

            builder.Property(x => x.ChunkNumber)
                .HasColumnName("chunk_number")
                .IsRequired();

            builder.Property(x => x.StorageUrl)
                .HasColumnName("storage_url")
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(x => x.StorageKey)
                .HasColumnName("storage_key")
                .HasMaxLength(1000);

            builder.Property(x => x.PublicId)
                .HasColumnName("public_id")
                .HasMaxLength(500);

            builder.Property(x => x.Size)
                .HasColumnName("size")
                .IsRequired();

            builder.Property(x => x.Checksum)
                .HasColumnName("checksum")
                .HasMaxLength(255);

            builder.Property(x => x.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(255);

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired();

            builder.Property(x => x.UploadAttemptCount)
                .HasColumnName("upload_attempt_count")
                .IsRequired();

            builder.Property(x => x.ReceivedAt)
                .HasColumnName("received_at")
                .IsRequired();

            builder.Property(x => x.UploadedAt)
                .HasColumnName("uploaded_at")
                .IsRequired();

            builder.Property(x => x.ErrorMessage)
                .HasColumnName("error_message")
                .HasMaxLength(2000);

            // PostgreSQL concurrency token
            builder.Property(r => r.xmin)
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();

            builder.HasOne(x => x.Recording)
                .WithMany(r => r.Chunks)
                .HasForeignKey(x => x.RecordingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.RecordingId, x.ChunkNumber })
                .IsUnique();

            builder.HasIndex(x => x.RecordingId);

            builder.HasIndex(x => x.Status);

            builder.HasIndex(x => new { x.RecordingId, x.ChunkNumber })
                    .IsUnique()
                    .HasDatabaseName("ux_recording_chunks_recording_id_chunk_number");
        }
    }
}
