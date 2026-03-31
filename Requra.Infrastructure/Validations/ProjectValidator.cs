using FluentValidation;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
   public class ProjectValidator : AbstractValidator<Project>
    {
        public ProjectValidator() { 
        
          RuleFor(p=>p.Name).NotEmpty().WithMessage("Project Name is required")
                .MaximumLength(100).WithMessage("Can not exceed 100 characters");

            RuleFor(p => p.Description).MaximumLength(500).WithMessage("The Description can not exceed 500 characters");


        }
    }
}
