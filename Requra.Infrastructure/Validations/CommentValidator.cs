using FluentValidation;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
    public class CommentValidator : AbstractValidator<Comment>
    {
        public CommentValidator() {
            RuleFor(x => x.Content)
           .NotEmpty().WithMessage("Comment cannot be empty.")
           .MaximumLength(500).WithMessage("Comment must be 500 characters or less");
          
        }
    }
}
