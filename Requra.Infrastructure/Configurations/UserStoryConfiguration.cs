using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

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
            //old
            //builder.Property(u => u.AcceptanceCriteria)
            //       .HasColumnName("acceptance_criteria")
            //       .HasColumnType("jsonb")
            //       .HasConversion(
            //           v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
            //           v => JsonSerializer.Deserialize<List<AcceptanceCriterion>>(v, (JsonSerializerOptions)null) ?? new List<AcceptanceCriterion>())
            //       .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<AcceptanceCriterion>>(
            //           (c1, c2) => c1.SequenceEqual(c2),
            //           c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            //           c => c.ToList()));

            builder.Property(u => u.AcceptanceCriteria)
                  .HasColumnName("acceptance_criteria")
                  .HasColumnType("jsonb")
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                      v => DeserializeAcceptanceCriteria(v))
                  .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<AcceptanceCriterion>>(
                      (c1, c2) => c1.SequenceEqual(c2),
                      c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                      c => c.ToList()));

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

            //builder.HasMany(us => us.Comments)
            //       .WithOne(c => c.UserStory)
            //       .HasForeignKey(c => c.UserStoryId)
            //       .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(us => us.Project)
                   .WithMany(p => p.UserStories)
                   .HasForeignKey(us => us.ProjectId)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.Property(us => us.ReviewFeedback)
       .HasColumnName("review_feedback")
       .HasColumnType("text");

            builder.Property(us => us.ReviewedById)
                   .HasColumnName("reviewed_by_id");

            builder.Property(us => us.ReviewedAt)
                   .HasColumnName("reviewed_at")
                   .HasColumnType("timestamptz");

            builder.Property(us => us.Version)
                   .HasColumnName("version")
                   .IsConcurrencyToken()
                   .HasDefaultValue(1)
                   .IsRequired();

        }
        private static List<AcceptanceCriterion> DeserializeAcceptanceCriteria(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<AcceptanceCriterion>();

            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return new List<AcceptanceCriterion>();

            var isLegacyStringArray = document.RootElement.EnumerateArray()
                .All(el => el.ValueKind == JsonValueKind.String);

            if (isLegacyStringArray)
            {
                return document.RootElement.EnumerateArray()
                    .Select(el => new AcceptanceCriterion(el.GetString() ?? string.Empty, null))
                    .ToList();
            }

            return JsonSerializer.Deserialize<List<AcceptanceCriterion>>(json, (JsonSerializerOptions)null)
                   ?? new List<AcceptanceCriterion>();
        }
    }
}
