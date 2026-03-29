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

            builder.HasKey(mp => new { mp.UserId, mp.MeetingId });

            builder.Property(mp => mp.UserId)
                   .HasColumnName("user_id")
                   .IsRequired();

            builder.Property(mp => mp.MeetingId)
                   .HasColumnName("meeting_id")
                   .IsRequired();

            builder.Property(mp => mp.Role)
                   .HasColumnName("role")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(mp => mp.JoinedAt)
                   .HasColumnName("joined_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

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
