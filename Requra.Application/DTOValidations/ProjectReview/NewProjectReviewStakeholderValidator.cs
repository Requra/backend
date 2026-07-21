using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOValidations.ProjectReview
{
    using FluentValidation;
    using Requra.Application.DTOs.ProjectReviewInvitaion;

    public class NewProjectReviewStakeholderValidator
        : AbstractValidator<NewProjectReviewStakeholderInput>
    {
        public NewProjectReviewStakeholderValidator()
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required.")
                .MinimumLength(2).WithMessage("Display name must be at least 2 characters long.")
                .MaximumLength(120).WithMessage("Display name cannot exceed 120 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.RoleTitle)
                .MaximumLength(160).WithMessage("Role title cannot exceed 160 characters.");

            RuleFor(x => x.Company)
                .MaximumLength(160).WithMessage("Company name cannot exceed 160 characters.");

        }
    }
}
