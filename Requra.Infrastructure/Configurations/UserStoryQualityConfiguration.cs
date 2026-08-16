using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Requra.Infrastructure.Configurations
{
    public class UserStoryQualityConfiguration : IEntityTypeConfiguration<UserStoryQuality>
    {
        public void Configure(EntityTypeBuilder<UserStoryQuality> builder)
        {
            builder.ToTable("user_story_qualities");

            builder.HasKey(q => q.Id);

            builder.Property(q => q.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(q => q.Score)
                   .HasColumnName("score");

            //builder.Property(q => q.Issues)
            //       .HasColumnName("issues")
            //       .HasColumnType("jsonb");

            //builder.Property(q => q.Warnings)
            //       .HasColumnName("warnings")
            //       .HasColumnType("jsonb");
            builder.Property(q => q.Issues)
                   .HasColumnName("issues")
                   .HasColumnType("jsonb")
                   .HasConversion(
                       v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                       v => string.IsNullOrWhiteSpace(v)
                            ? new List<string>()
                            : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>())
                   .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                       (c1, c2) => c1.SequenceEqual(c2),
                       c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                       c => c.ToList()));

            builder.Property(q => q.Warnings)
                   .HasColumnName("warnings")
                   .HasColumnType("jsonb")
                   .HasConversion(
                       v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                       v => string.IsNullOrWhiteSpace(v)
                            ? new List<string>()
                            : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>())
                   .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                       (c1, c2) => c1.SequenceEqual(c2),
                       c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                       c => c.ToList()));

            builder.Property(q => q.QualityStatus)
                   .HasColumnName("quality_status")
                   .HasConversion<string>()
                   .HasDefaultValue(QualityStatus.NOT_EVALUATED)
                   .IsRequired();
            builder.Property(q => q.QualityStatus)
       .HasSentinel(QualityStatus.NOT_EVALUATED);

            builder.Property(q => q.UserStoryId)
                   .HasColumnName("user_story_id")
                   .IsRequired();

            builder.HasOne(q => q.UserStory)
                   .WithOne(us => us.Quality)
                   .HasForeignKey<UserStoryQuality>(q => q.UserStoryId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
