using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Response;
using static System.Net.WebRequestMethods;

namespace Requra.Presentation.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService, IValidator<RegisterRequestDto> validator) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            var validation = await validator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(Response<object>.Failure("Validation failed", 400, errors));
            }
            var result = await authService.RegisterAsync(request);
            return StatusCode(result.StatusCode, result);
        }
    }  
}

