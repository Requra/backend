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

        public Guid UserStoryId { get; private set; }

        public string AuthorId { get; private set; } = null!;

        public Guid? ParentCommentId { get; private set; }

        public string Content { get; private set; } = null!;

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        // Navigation
        public UserStory UserStory { get; private set; } = null!;
        public ApplicationUser Author { get; private set; } = null!;
        public Comment? ParentComment { get; private set; }
        public ICollection<Comment> Replies { get; private set; } = new List<Comment>();

        // Constructor
        public Comment(Guid userStoryId, string authorId, string content, Guid? parentCommentId = null)
        {
            Id = Guid.NewGuid();
            UserStoryId = userStoryId;
            AuthorId = authorId;
            Content = content;
            ParentCommentId = parentCommentId;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateContent(string newContent)
        {
            Content = newContent;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddReply(Comment reply)
        {
            Replies.Add(reply);
        }
    }
}
