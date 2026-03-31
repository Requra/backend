using FluentValidation;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
    public class RequirementValidator: AbstractValidator<Requirement>
    {
        public RequirementValidator() {
            RuleFor(rq => rq.Title).NotEmpty().WithMessage("Requirement title is required")
               .MaximumLength(100).WithMessage("Can not exceed 100 characters");

            RuleFor(rq => rq.Description).MaximumLength(500).WithMessage("The Description can not exceed 500 characters");

        }
    }
}
