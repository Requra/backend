using FluentValidation;
using Requra.Application.DTOs.Project.ProjectCreation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
    public class TeamMemberValidator : AbstractValidator<TeamMemberDto>
    {
        public TeamMemberValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Team member email is required")
                .EmailAddress().WithMessage("Invalid team member email");
        }
    }
}
