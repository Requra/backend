using FluentValidation;
using Requra.Application.DTOs.Project.ProjectUpdate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
 
public class UpdateProjectValidator : AbstractValidator<ProjectUpdateRequestDto>
    {
        public UpdateProjectValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name cannot be empty")
                .MaximumLength(100).WithMessage("Project name must not exceed 100 characters")
                .When(x => x.Name != null);

            // (optional)
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
                .When(x => x.Description != null);

            RuleFor(x => x.ClientEmail)
                .EmailAddress().WithMessage("Invalid client email format")
                .When(x => !string.IsNullOrWhiteSpace(x.ClientEmail));

            RuleFor(x => x.ProjectType)
                .IsInEnum().WithMessage("Invalid project type")
                .When(x => x.ProjectType.HasValue);

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid project status")
                .When(x => x.Status.HasValue);

            RuleFor(x => x.Language)
                .IsInEnum().WithMessage("Invalid language value")
                .When(x => x.Language.HasValue);


            RuleFor(x => x.TeamMembers)
               .Must(list => list?.Select(x => x.Email.ToLower()).Distinct().Count() == list?.Count)
               .WithMessage("Duplicate team member emails are not allowed");

            RuleForEach(x => x.TeamMembers)
                .SetValidator(new TeamMemberValidator());
        }
    }
}

