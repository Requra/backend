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

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(c => c.UserStoryId)
                   .HasColumnName("user_story_id")
                   .IsRequired();

            builder.Property(c => c.AuthorId)
                   .HasColumnName("author_id")
                   .IsRequired();

            builder.Property(c => c.ParentCommentId)
                   .HasColumnName("parent_comment_id");

            builder.Property(c => c.Content)
                   .HasColumnName("content")
                   .HasColumnType("text")
                   .IsRequired();

            builder.Property(c => c.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            builder.Property(c => c.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            builder.HasOne(c => c.UserStory)
                   .WithMany(us => us.Comments)
                   .HasForeignKey(c => c.UserStoryId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Author)
                   .WithMany(u => u.Comments)
                   .HasForeignKey(c => c.AuthorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.ParentComment)
                   .WithMany(c => c.Replies)
                   .HasForeignKey(c => c.ParentCommentId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
