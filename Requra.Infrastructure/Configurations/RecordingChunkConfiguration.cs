using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class RecordingChunkConfiguration
        : IEntityTypeConfiguration<RecordingChunk>
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
            builder.Property(x => x.PublicId)
                .HasColumnName("public_id")
                .HasMaxLength(500);

            builder.Property(x => x.Size)
                .HasColumnName("size")
                .IsRequired();

            builder.Property(x => x.UploadedAt)
                .HasColumnName("uploaded_at")
                .IsRequired();

            builder.HasOne(x => x.Recording)
                .WithMany(r => r.Chunks)
                .HasForeignKey(x => x.RecordingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate chunks
            builder.HasIndex(x => new
            {
                x.RecordingId,
                x.ChunkNumber
            })
            .IsUnique();

            builder.HasIndex(x => x.RecordingId);
        }
    }
}
