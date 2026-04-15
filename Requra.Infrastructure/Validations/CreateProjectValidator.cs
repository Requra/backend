using FluentValidation;
using Requra.Application.DTOs.Project.ProjectCreation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
    public class CreateProjectRequestDtoValidator : AbstractValidator<ProjectRequestDto>
    {
        public CreateProjectRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name is required")
                .MinimumLength(3).WithMessage("Project name must be at least 3 characters")
                .MaximumLength(100).WithMessage("Project name must not exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
                .When(x => x.Description != null);

            RuleFor(x => x.ClientEmail)
                .NotEmpty().WithMessage("Client email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.ProjectType)
                .IsInEnum().WithMessage("Invalid project type");

            RuleFor(x => x.TeamMembers)
                .NotNull().Must(list => list.Select(x => x.Email.ToLower()).Distinct().Count() == list.Count)
                .WithMessage("Duplicate team member emails are not allowed");

            RuleForEach(x => x.TeamMembers)
                .SetValidator(new TeamMemberDtoValidator());
        }
    }
}
