using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("idempotency_records");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(r => r.Key)
                   .HasColumnName("key")
                   .HasMaxLength(128)
                   .IsRequired();

            builder.Property(r => r.Scope)
                   .HasColumnName("scope")
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(r => r.RequestHash)
                   .HasColumnName("request_hash")
                   .HasMaxLength(64)
                   .IsRequired();

            builder.Property(r => r.ResponseBody)
                   .HasColumnName("response_body")
                   .HasColumnType("jsonb")
                   .IsRequired();

            builder.Property(r => r.StatusCode)
                   .HasColumnName("status_code")
                   .IsRequired();

            builder.Property(r => r.CreatedAt)
                   .HasColumnName("created_at")
                   .IsRequired();

            // A client is expected to generate a unique key per logical operation; scoping
            // uniqueness by Key alone (not Key+Scope) matches "reusing the same key" in the
            // API contract literally, and is simpler for clients to reason about.
            builder.HasIndex(r => r.Key)
                   .IsUnique();
        }
    }
}
