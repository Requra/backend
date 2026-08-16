using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class AcceptanceCriterion
    {
        public Guid Id { get; private set; }
        public string? SourceAcceptanceCriterionId { get; private set; }

        public string Text { get; private set; } = null!;

        public string? CriterionType { get; private set; }

        public Guid UserStoryId { get; private set; }

        public UserStory UserStory { get; private set; } = null!;

        // Parameterless constructor for EF Core and JSON deserialization
        private AcceptanceCriterion()
        {
        }

        public AcceptanceCriterion(
      string text,
      string? criterionType,
      string? sourceAcceptanceCriterionId = null)
        {
            Id = Guid.NewGuid();

            Text = text;
            CriterionType = criterionType;
            SourceAcceptanceCriterionId = sourceAcceptanceCriterionId;
        }
    }
}
