using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Requra.Domain.Entities
{
    public class Comment
    {
        public Guid Id { get; private set; }

        public Guid ProjectId { get; private set; }
        public FeedbackTargetType TargetType { get; private set; }
        public Guid TargetId { get; private set; }
        public string? TargetTitle { get; private set; }

        public string AuthorId { get; private set; } = null!;
        public Guid? ParentCommentId { get; private set; }

        public StakeholderFeedbackStatus Status { get; private set; }
        public bool IsRead { get; private set; }

        public string Content { get; private set; } = null!;

        public string? ResolutionNote { get; private set; }
        public string? ResolvedById { get; private set; }
        public DateTime? ResolvedAt { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // Navigation
        public ApplicationUser Author { get; private set; } = null!;
        public Comment? ParentComment { get; private set; }
        public ICollection<Comment> Replies { get; private set; } = new List<Comment>();

        private Comment()
        {
        }

        public Comment(Guid projectId,FeedbackTargetType targetType,Guid targetId,string? targetTitle,string authorId,string content,Guid? parentCommentId = null)
        {
            Id = Guid.NewGuid();
            ProjectId = projectId;
            TargetType = targetType;
            TargetId = targetId;
            TargetTitle = targetTitle;
            AuthorId = authorId;
            Content = content;
            ParentCommentId = parentCommentId;

            Status = StakeholderFeedbackStatus.OPEN;
            IsRead = false;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateContent(string newContent)
        {
            Content = newContent;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsRead()
        {
            IsRead = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Resolve(string resolvedById, string? resolutionNote)
        {
            Status = StakeholderFeedbackStatus.RESOLVED;
            IsRead = true;
            ResolutionNote = resolutionNote;
            ResolvedById = resolvedById;
            ResolvedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reopen()
        {
            Status = StakeholderFeedbackStatus.OPEN;
            ResolutionNote = null;
            ResolvedById = null;
            ResolvedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddReply(Comment reply)
        {
            Replies.Add(reply);
        }

        public void MarkAsUnread()
        {
            IsRead = false;
            UpdatedAt = DateTime.UtcNow;
        }


    }
}
