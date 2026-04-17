using FluentValidation;
using Requra.Application.DTOs.Auth.Register;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
    

    public class RegisterValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(70).WithMessage("Name cannot exceed 100 characters.")
                .Matches(@"^[a-zA-Z0-9_]*$").WithMessage("name format must be alphanumeric characters and underscores.")
                .MinimumLength(3).WithMessage("The length of Name must be at least 3 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email address.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.");


            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm Password is required.")
                .Equal(x => x.Password).WithMessage("Passwords do not match.");
    

            RuleFor(x => x.Role)
                 .Must(role => role != UserRole.None)
                .IsInEnum().WithMessage("Invalid role selected.");
                
        }
    }
}
