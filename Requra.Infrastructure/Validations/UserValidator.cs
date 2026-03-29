using FluentValidation;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
    public class UserValidator : AbstractValidator<ApplicationUser>
    {
        public UserValidator()
        {
            RuleFor(u => u.UserName)
                .NotEmpty().WithMessage("User Name is required")
                .MaximumLength(100).WithMessage("First Name must not exceed 100 characters");


            RuleFor(u => u.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

        }
    }
}
