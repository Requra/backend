using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class MeetingSessionConfiguration : IEntityTypeConfiguration<MeetingSession>
    {
        public void Configure(EntityTypeBuilder<MeetingSession> builder)
        {
            builder.ToTable("meeting_sessions");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(m => m.SessionToken)
                   .HasColumnName("session_token");

            builder.Property(m => m.HostId)
                   .HasColumnName("host_id")
                   .IsRequired();

            builder.Property(m => m.StartedAt)
                   .HasColumnName("started_at")
                   .HasColumnType("timestamptz");

            builder.Property(m => m.EndedAt)
                   .HasColumnName("ended_at")
                   .HasColumnType("timestamptz");

            builder.Property(m => m.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(m => m.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            builder.Property(m => m.PlatformUrl)
                   .HasColumnName("platform_url");


            builder.HasOne(m => m.Host)
                   .WithMany(u => u.HostedMeetings)
                   .HasForeignKey(m => m.HostId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(m => m.Documents)
                   .WithOne(d => d.Meeting)
                   .HasForeignKey(d => d.MeetingId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
