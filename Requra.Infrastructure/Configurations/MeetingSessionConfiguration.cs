using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;

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
                .ValueGeneratedNever();

            builder.Property(m => m.SessionToken)
                .HasColumnName("session_token")
                .HasMaxLength(500);

            builder.Property(m => m.Title)
                .HasColumnName("title")
                .HasMaxLength(250);

            builder.Property(m => m.Description)
                .HasColumnName("description");

            builder.Property(m => m.HostId)
                .HasColumnName("host_id")
                .IsRequired();

            builder.Property(m => m.CreatedById)
                .HasColumnName("created_by_id")
                .IsRequired();

            builder.Property(m => m.ProjectId)
                .HasColumnName("project_id")
                .IsRequired();

            builder.Property(m => m.PlatformUrl)
                .HasColumnName("platform_url")
                .HasMaxLength(1000);

            builder.Property(m => m.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(m => m.TranscriptStatus)
                .HasColumnName("transcript_status")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(m => m.StartedAt)
                .HasColumnName("started_at")
                .HasColumnType("timestamptz");

            builder.Property(m => m.EndedAt)
                .HasColumnName("ended_at")
                .HasColumnType("timestamptz");

            builder.Property(m => m.ScheduledAt)
                .HasColumnName("scheduled_at")
                .HasColumnType("timestamptz");

            builder.Property(m => m.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamptz");

            builder.Property(m => m.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("NOW()");

            builder.Property(m => m.DurationMinutes)
                .HasColumnName("duration_minutes");

            // =========================
            // Indexes
            // =========================

            builder.HasIndex(m => m.ProjectId);

            builder.HasIndex(m => m.HostId);

            builder.HasIndex(m => m.CreatedById);

            builder.HasIndex(m => m.Status);

            builder.HasIndex(m => m.ScheduledAt);

            // =========================
            // Relationships
            // =========================

            builder.HasOne(m => m.Project)
                .WithMany()
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.Host)
                .WithMany()
                .HasForeignKey(m => m.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.CreatedBy)
                .WithMany()
                .HasForeignKey(m => m.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(m => m.Documents)
                .WithOne(d => d.Meeting)
                .HasForeignKey(d => d.MeetingId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(m => m.Participants)
                .WithOne(mp => mp.Meeting)
                .HasForeignKey(mp => mp.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(m => m.Recordings)
                .WithOne(r => r.Meeting)
                .HasForeignKey(r => r.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}