using FluentValidation;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
    public class UserStoryValidator : AbstractValidator<UserStory>
    {
        public UserStoryValidator() {

            RuleFor(us => us.Title).NotEmpty().WithMessage("User story titleis required")
                   .MaximumLength(100).WithMessage("Can not exceed 100 characters");

            RuleFor(us => us.Description).MaximumLength(500).WithMessage("The Description can not exceed 500 characters");
        }
    }
}
