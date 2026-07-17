using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            builder.ToTable("comments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.ProjectId)
                .HasColumnName("project_id")
                .IsRequired();

            builder.Property(x => x.TargetType)
                .HasColumnName("target_type")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.TargetId)
                .HasColumnName("target_id")
                .IsRequired();

            builder.Property(x => x.TargetTitle)
                .HasColumnName("target_title")
                .HasMaxLength(300);

            builder.Property(x => x.AuthorId)
                .HasColumnName("author_id")
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.ParentCommentId)
                .HasColumnName("parent_comment_id");

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.IsRead)
                .HasColumnName("is_read")
                .IsRequired();

            builder.Property(x => x.Content)
                .HasColumnName("content")
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(x => x.ResolutionNote)
                .HasColumnName("resolution_note")
                .HasMaxLength(4000);

            builder.Property(x => x.ResolvedById)
                .HasColumnName("resolved_by_id")
                .HasMaxLength(450);

            builder.Property(x => x.ResolvedAt)
                .HasColumnName("resolved_at");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.HasIndex(x => x.ProjectId)
                .HasDatabaseName("ix_comments_project_id");

            builder.HasIndex(x => new { x.TargetType, x.TargetId })
                .HasDatabaseName("ix_comments_target_type_target_id");

            builder.HasIndex(x => x.AuthorId)
                .HasDatabaseName("ix_comments_author_id");

            builder.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ParentComment)
                .WithMany(x => x.Replies)
                .HasForeignKey(x => x.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    
    }
}
