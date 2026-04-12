using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Auth.RefreshToken;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Response;
using Requra.Infrastructure.Services.AuthService;
using static System.Net.WebRequestMethods;

namespace Requra.Presentation.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService, IValidator<RegisterRequestDto> validator,IValidator<RefreshTokenRequestDto> refreshTokenValidator) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            var validation = await validator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(Response<string>.Failure("Validation failed", 400, errors));
            }
            var result = await authService.RegisterAsync(request);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto request)
        {
            var validation = await refreshTokenValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(Response<RefreshTokenResponseDto>.Failure("Validation failed", 400, errors));
            }
            var result = await authService.RefreshTokenAsync(request);
            return StatusCode(result.StatusCode, result);
        }
    }  
}

