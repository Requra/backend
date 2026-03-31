using FluentValidation;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
    public class ApprovalValidator : AbstractValidator<Approval>
    {
        public ApprovalValidator() { 
          RuleFor(x=>x.Notes).MaximumLength(500).WithMessage("Notes can not exceed 500 characters");
        
        }
    }
}
