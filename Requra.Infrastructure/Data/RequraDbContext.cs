using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Requra.Infrastructure.Data
{
    public class RequraDbContext : IdentityDbContext<ApplicationUser>
    {
        public RequraDbContext(DbContextOptions<RequraDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<UserSubscription>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.UserId).IsUnique();
                entity.HasIndex(x => x.StripeCustomerId).IsUnique();
                entity.HasIndex(x => x.StripeSubscriptionId).IsUnique();

                entity.Property(x => x.StripeCustomerId).HasMaxLength(200);
                entity.Property(x => x.StripeSubscriptionId).HasMaxLength(200);
                entity.Property(x => x.StripeProductId).HasMaxLength(200);
                entity.Property(x => x.StripePriceId).HasMaxLength(200);
                entity.Property(x => x.StripeCheckoutSessionId).HasMaxLength(200);

                entity.HasOne(x => x.User)
                    .WithOne()
                    .HasForeignKey<UserSubscription>(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<StripeWebhookEvent>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.StripeEventId).IsUnique();

                entity.Property(x => x.StripeEventId).HasMaxLength(200);
                entity.Property(x => x.EventType).HasMaxLength(200);
                entity.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
            });

            builder.ApplyConfigurationsFromAssembly(typeof(RequraDbContext).Assembly);

            base.OnModelCreating(builder);
        }

        public DbSet<ApplicationUser> ApplicationUser { get; set; }
        public DbSet<Approval> Approvals { get; set; }
        public DbSet<AIModel> AIModels { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentModel> DocumentModels { get; set; }
        public DbSet<DocumentRequirement> DocumentRequirements { get; set; }
        public DbSet<MeetingSession> MeetingSessions { get; set; }
        public DbSet<MeetingParticipant> MeetingParticipants { get; set; }
        public DbSet<UserStory> UserStories { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<Requirement> Requirements { get; set; }
        public DbSet<Summary> Summaries { get; set; }
        public DbSet<Recording> Recordings { get; set; }
        public DbSet<RecordingChunk> RecordingChunks { get; set; }

        public DbSet<AnalysisRun> AnalysisRuns { get; set; }
        public DbSet<AnalysisResult> AnalysisResults { get; set; }
        public DbSet<Invitation> Invitations { get; set; }

        public DbSet<ProjectReviewInvitation> ProjectReviewInvitations { get; set; }
        //new tablefor idempotency records
        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }

        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<StripeWebhookEvent> StripeWebhookEvents {  get; set; }



    }
}
