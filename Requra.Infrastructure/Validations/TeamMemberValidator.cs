using FluentValidation;
using Requra.Application.DTOs.Project.ProjectCreation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
    public class TeamMemberDtoValidator : AbstractValidator<TeamMemberDto>
    {
        public TeamMemberDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Team member email is required")
                .EmailAddress().WithMessage("Invalid team member email");
        }
    }
}
