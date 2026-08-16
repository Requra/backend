using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System.Text.Json;

namespace Requra.Infrastructure.Configurations
{
    public class UserStorySourceRefConfiguration : IEntityTypeConfiguration<UserStorySourceRef>
    {
        public void Configure(EntityTypeBuilder<UserStorySourceRef> builder)
        {
            builder.ToTable("user_story_source_refs");

            builder.HasKey(ref_ => ref_.Id);

            builder.Property(ref_ => ref_.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(ref_ => ref_.Page)
                   .HasColumnName("page")
                   .HasColumnType("text")
                   .HasConversion(
                       v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                       v => v == null ? null : JsonSerializer.Deserialize<object>(v, (JsonSerializerOptions)null));

            builder.Property(ref_ => ref_.Quote)
                   .HasColumnName("quote")
                   .HasColumnType("text");

            builder.Property(ref_ => ref_.ChunkId)
                   .HasColumnName("chunk_id")
                   .HasColumnType("text");

            builder.Property(ref_ => ref_.SourceId)
                   .HasColumnName("source_id")
                   .HasColumnType("text");

            builder.Property(ref_ => ref_.SourceType)
                   .HasColumnName("source_type")
                   .HasColumnType("text");

            builder.Property(ref_ => ref_.DocumentName)
                   .HasColumnName("document_name")
                   .HasColumnType("text");

            builder.Property(ref_ => ref_.ConfidenceScore)
                   .HasColumnName("confidence_score")
                   .HasColumnType("double precision");

            builder.Property(ref_ => ref_.UserStoryId)
                   .HasColumnName("user_story_id")
                   .IsRequired();

            builder.HasOne(ref_ => ref_.UserStory)
                   .WithMany(us => us.SourceRefs)
                   .HasForeignKey(ref_ => ref_.UserStoryId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
