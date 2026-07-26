using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class MeetingParticipantConfiguration : IEntityTypeConfiguration<MeetingParticipant>
    {
        public void Configure(EntityTypeBuilder<MeetingParticipant> builder)
        {
            builder.ToTable("meeting_participants");

            builder.HasKey(mp => mp.Id);

            builder.Property(mp => mp.Id)
                   .HasColumnName("id");

            builder.Property(mp => mp.MeetingId)
                   .HasColumnName("meeting_id")
                   .IsRequired();

            // Nullable: guests who join without an account have no UserId.
            builder.Property(mp => mp.UserId)
                   .HasColumnName("user_id");

            builder.Property(mp => mp.DisplayName)
                   .HasColumnName("display_name")
                   .HasMaxLength(200);

            builder.Property(mp => mp.Email)
                   .HasColumnName("email")
                   .HasMaxLength(256);

            builder.Property(mp => mp.Role)
                   .HasColumnName("role")
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();

            builder.Property(mp => mp.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();

            builder.Property(mp => mp.RecordingConsent)
                   .HasColumnName("recording_consent")
                   .HasDefaultValue(false)
                   .IsRequired();

            builder.Property(mp => mp.ConsentedAt)
                   .HasColumnName("consented_at");

            builder.Property(mp => mp.JoinedAt)
                   .HasColumnName("joined_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            builder.Property(mp => mp.LeftAt)
                   .HasColumnName("left_at");

            builder.HasIndex(mp => mp.MeetingId)
                   .HasDatabaseName("ix_meeting_participants_meeting_id");

            builder.HasIndex(mp => new { mp.MeetingId, mp.UserId })
                   .HasDatabaseName("ix_meeting_participants_meeting_user");

            // Relationships
            builder.HasOne(mp => mp.User)
                   .WithMany(u=>u.MeetingParticipations)
                   .HasForeignKey(mp => mp.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(mp => mp.Meeting)
                   .WithMany(m=>m.Participants)
                   .HasForeignKey(mp => mp.MeetingId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
