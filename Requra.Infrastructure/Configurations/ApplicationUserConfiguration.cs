using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("users");

            builder.Property(u => u.FullName)
                   .HasColumnName("full_name")
                   .HasMaxLength(255);

            builder.Property(u => u.PreferredLanguage)
                   .HasColumnName("preferred_language")
                   .HasConversion<string>();

            builder.Property(u => u.AvatarUrl)
                   .HasColumnName("avatar_url");


            builder.Property(u => u.IsActive)
                   .HasColumnName("is_active")
                   .HasDefaultValue(true);

            builder.Property(u => u.LastLoginAt)
                   .HasColumnName("last_login_at")
                   .HasColumnType("timestamptz");

            builder.Property(u => u.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            // UpdatedAt
            builder.Property(u => u.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            

            builder.HasMany(u => u.Projects)
                   .WithOne(p => p.Owner)
                   .HasForeignKey(p => p.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.UploadedDocuments)
                   .WithOne(d => d.Uploader)
                   .HasForeignKey(d => d.UploadedById)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.HostedMeetings)
                   .WithOne(m => m.Host)
                   .HasForeignKey(m => m.HostId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.CreatedUserStories)
                   .WithOne(us => us.Creator)
                   .HasForeignKey(us => us.CreatedAt)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Comments)
                   .WithOne(c => c.Author)
                   .HasForeignKey(c => c.AuthorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Approvals)
                   .WithOne(a => a.Reviewer)
                   .HasForeignKey(a => a.ReviewerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
