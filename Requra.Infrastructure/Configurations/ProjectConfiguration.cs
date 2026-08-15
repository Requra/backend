using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("projects");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");


            builder.Property(p => p.Name)
                   .HasColumnName("name")
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(p => p.Description)
                   .HasColumnName("description")
                   .HasColumnType("text");

            builder.Property(p => p.Language)
                   .HasColumnName("language")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(p => p.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(p => p.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            builder.Property(p => p.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasColumnType("timestamptz")
                   .HasDefaultValueSql("NOW()");

            builder.Property(p => p.ProjectType)
                   .HasColumnName("Project_type")
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(p => p.IsDeleted)
                   .HasColumnName("is_deleted")
                   .HasDefaultValue(false);

            // ClickUp Integration Configuration
            builder.Property(p => p.IsClickUpConnected)
                   .HasColumnName("is_click_up_connected")
                   .HasDefaultValue(false);

            builder.Property(p => p.ClickUpAccessToken)
                   .HasColumnName("click_up_access_token")
                   .HasColumnType("text");

            builder.Property(p => p.ClickUpTeamId)
                   .HasColumnName("click_up_team_id")
                   .HasMaxLength(50);

            builder.Property(p => p.ClickUpSpaceId)
                   .HasColumnName("click_up_space_id")
                   .HasMaxLength(50);

            builder.Property(p => p.ClickUpListId)
                   .HasColumnName("click_up_list_id")
                   .HasMaxLength(50);

            builder.Property(p => p.ClickUpTokenExpiresAt)
                   .HasColumnName("click_up_token_expires_at")
                   .HasColumnType("timestamptz");

            builder.HasMany(p => p.Documents)
                   .WithOne(d => d.Project)
                   .HasForeignKey(d => d.ProjectId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Members)
                   .WithOne(m => m.Project)
                   .HasForeignKey(m => m.ProjectId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
