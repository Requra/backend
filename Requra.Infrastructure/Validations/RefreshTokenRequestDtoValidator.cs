using FluentValidation;
using Requra.Application.DTOs.Auth.RefreshToken;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Validations
{
    public class RefreshTokenRequestDtoValidator :AbstractValidator<RefreshTokenRequestDto>
    {
        public RefreshTokenRequestDtoValidator()
        {
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithMessage("Access token is required");

            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required");
        }
    }
}
