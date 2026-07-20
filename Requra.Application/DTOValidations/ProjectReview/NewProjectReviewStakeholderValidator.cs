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
                .NotEmpty()
                .MinimumLength(2)
                .MaximumLength(120);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

        }
    }
}
