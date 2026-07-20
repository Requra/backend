using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOValidations.ProjectReview
{
    using FluentValidation;
    using Requra.Application.DTOs.ProjectReviewInvitaion;
    using Requra.Domain.Enums;

    public class CreateProjectReviewInvitationValidator
        : AbstractValidator<CreateProjectReviewInvitationRequest>
    {
        public CreateProjectReviewInvitationValidator()
        {
            RuleFor(x => x)
                .Must(x =>
                    (x.StakeholderIds != null && x.StakeholderIds.Any()) ||
                    (x.Stakeholders != null && x.Stakeholders.Any()))
                .WithMessage("You must provide at least one stakeholderId or stakeholder.");

            RuleForEach(x => x.StakeholderIds)
                .NotEmpty()
                .WithMessage("StakeholderId must be a valid GUID.");

            RuleForEach(x => x.Stakeholders)
                .SetValidator(new NewProjectReviewStakeholderValidator());

            RuleFor(x => x.Permission)
                .IsInEnum()
                .WithMessage("Invalid permission value.");

            RuleFor(x => x.ExpiresAt)
                .Must(date => !date.HasValue || date.Value > DateTime.UtcNow)
                .WithMessage("Expiration must be in the future.");
        }
    }
}
