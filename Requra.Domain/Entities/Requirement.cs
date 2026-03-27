using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Requra.Domain.Entities
{
    public class Requirement
    {
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(100)]
        public RequirementType Type { get; set; }= RequirementType.Functional;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? Language { get; set; }

        // Navigation properties
        public virtual ICollection<DocumentRequirement> DocumentRequirements { get; set; } = new List<DocumentRequirement>();
        public virtual ICollection<UserStory> UserStories { get; set; } = new List<UserStory>();
        public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    }
}
