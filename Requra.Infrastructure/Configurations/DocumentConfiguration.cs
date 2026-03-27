
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    internal class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("documents");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(d => d.ProjectId)
                   .HasColumnName("project_id")
                   .IsRequired();

            builder.Property(d => d.UploadedById)
                   .HasColumnName("uploaded_by")
                   .IsRequired();

            builder.Property(d => d.MeetingId)
                   .HasColumnName("meeting_id");

            builder.Property(d => d.Title)
                   .HasColumnName("title")
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(d => d.Type)
                   .HasColumnName("type")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(d => d.Language)
                   .HasColumnName("language")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(d => d.StorageUrl)
                   .HasColumnName("storage_url");

            builder.Property(d => d.FileSize)
                   .HasColumnName("file_size");

            builder.Property(d => d.TranscriptText)
                   .HasColumnName("transcript_text")
                   .HasColumnType("text");

            builder.Property(d => d.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(d => d.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            builder.Property(d => d.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");


            builder.HasOne(d => d.Project)
                   .WithMany(p => p.Documents)
                   .HasForeignKey(d => d.ProjectId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Uploader)
                   .WithMany(u => u.UploadedDocuments)
                   .HasForeignKey(d => d.UploadedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.Meeting)
                   .WithMany(m => m.Documents)
                   .HasForeignKey(d => d.MeetingId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(d => d.DocumentModels)
                   .WithOne(dm => dm.Document)
                   .HasForeignKey(dm => dm.DocumentId);

            builder.HasMany(d => d.DocumentRequirements)
                   .WithOne(dr => dr.Document)
                   .HasForeignKey(dr => dr.DocumentId);

            builder.HasMany(d => d.Summaries)
                   .WithOne(s => s.Document)
                   .HasForeignKey(s => s.DocumentId);
        }
    }
}
