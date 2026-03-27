using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class UserStoryConfiguration : IEntityTypeConfiguration<UserStory>
    {
        public void Configure(EntityTypeBuilder<UserStory> builder)
        {
            builder.ToTable("user_stories");

            builder.HasKey(us => us.Id);

            builder.Property(us => us.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(us => us.Title)
                   .HasColumnName("title")
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(us => us.Description)
                   .HasColumnName("description")
                   .HasColumnType("text");

            builder.Property(us => us.AcceptanceCriteria)
                   .HasColumnName("acceptance_criteria")
                   .HasColumnType("text");

            builder.Property(us => us.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(us => us.Priority)
                   .HasColumnName("priority")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(us => us.Language)
                   .HasColumnName("language")
                   .HasConversion<string>();

            builder.Property(us => us.CreatorId)
                   .HasColumnName("creator_id")
                   .IsRequired();

            builder.Property(us => us.RequirementId)
                   .HasColumnName("requirement_id")
                   .IsRequired();

            builder.Property(us => us.JiraTicket)
                   .HasColumnName("jira_ticket")
                   .HasMaxLength(100);

            builder.Property(us => us.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            builder.Property(us => us.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

           

            builder.HasOne(us => us.Creator)
                   .WithMany(u => u.CreatedUserStories)
                   .HasForeignKey(us => us.CreatorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(us => us.Requirement)
                   .WithMany(r => r.UserStories)
                   .HasForeignKey(us => us.RequirementId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(us => us.Comments)
                   .WithOne(c => c.UserStory)
                   .HasForeignKey(c => c.UserStoryId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
