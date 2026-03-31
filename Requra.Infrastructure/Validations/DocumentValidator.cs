using FluentValidation;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
    public class DocumentValidator : AbstractValidator<Document> 
    {
        public DocumentValidator() {
            RuleFor(d => d.Title).NotEmpty().WithMessage("Document title is required")
                .MaximumLength(50).WithMessage("The title can not exceed 50 characters");
        }
    }
}
