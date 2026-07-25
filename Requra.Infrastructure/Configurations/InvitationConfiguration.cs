using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class MeetingInvitationConfiguration : IEntityTypeConfiguration<Invitation>
    {
        public void Configure(EntityTypeBuilder<Invitation> builder)
        {
            builder.ToTable("invitations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.MeetingId)
                .HasColumnName("meeting_id");

            builder.Property(x => x.InviteType)
                .HasColumnName("invite_type")
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(200);

            builder.Property(x => x.ProjectMemberId)
                .HasColumnName("project_member_id")
                .HasMaxLength(450);

            builder.Property(x => x.StakeholderId)
                .HasColumnName("stakeholder_id")
                .HasMaxLength(450);

            builder.Property(x => x.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.InvitedById)
                .HasColumnName("invited_by_id")
                .HasMaxLength(450)
                .IsRequired();

            builder.HasIndex(x => x.InviteToken)
                .IsUnique()
                .HasDatabaseName("ix_meeting_invitations_invite_token");

            builder.Property(x => x.ExpiresAt)
                .HasColumnName("expires_at");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasIndex(x => x.MeetingId)
                .HasDatabaseName("ix_meeting_invitations_meeting_id");

            builder.HasIndex(x => x.Email)
                .HasDatabaseName("ix_meeting_invitations_email");

            builder.HasIndex(x => new { x.MeetingId, x.Email, x.Role, x.Status })
                .HasDatabaseName("ix_meeting_invitations_meeting_email_role_status");

            builder.HasOne(x => x.Meeting)
                .WithMany(x => x.Invitations)
                .HasForeignKey(x => x.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.InvitedBy)
                .WithMany(x => x.Invitations)
                .HasForeignKey(x => x.InvitedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
