using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Auth.Login;
using Requra.Application.DTOs.Auth.RefreshToken;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Response;
using Requra.Infrastructure.ExternalDTOs.ExternalAuth.GoogleAuthDTO;
using Requra.Infrastructure.ExternalInterfaces.IExternalAuth;
using Requra.Infrastructure.Services.AuthService;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace Requra.Presentation.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService,IGoogleAuthService googleAuthService,IConfiguration configuration, ILogger<AuthController> logger) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
           
            var result = await authService.RegisterAsync(request);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto request)
        {
            // Validators should be more clean later
           
            var result = await authService.RefreshTokenAsync(request);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await authService.LogoutAsync(userId);

            return StatusCode(result.StatusCode, result);
        }


        [HttpPost("login")]
        public async Task<ActionResult<Response<LogInResponseDTO>>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(
                    Response<LogInResponseDTO>.Failure(
                        new LogInResponseDTO(),
                        "Validation failed",
                        400,
                        validationErrors));
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(
                    Response<LogInResponseDTO>.Failure(
                        new LogInResponseDTO(),
                        "Email is required",
                        400));
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(
                    Response<LogInResponseDTO>.Failure(
                        new LogInResponseDTO(),
                        "Password is required",
                        400));
            }

            try
            {
                var result = await authService.LoginAsync(request);

                return result.StatusCode switch
                {
                    200 => Ok(
                        Response<LogInResponseDTO>.Success(
                            result.Data!,
                            result.Message,
                            200)),

                    401 => Unauthorized(
                        Response<LogInResponseDTO>.Failure(
                            new LogInResponseDTO(),
                            result.Message,
                            401)),

                    403 => StatusCode(
                        403,
                        Response<LogInResponseDTO>.Failure(
                            new LogInResponseDTO(),
                            result.Message,
                            403)),

                    404 => NotFound(
                        Response<LogInResponseDTO>.Failure(
                            new LogInResponseDTO(),
                            result.Message,
                            404)),

                    400 => BadRequest(
                        Response<LogInResponseDTO>.Failure(
                            new LogInResponseDTO(),
                            result.Message,
                            400,
                            result.Errors)),

                    500 => StatusCode(
                        500,
                        Response<LogInResponseDTO>.Failure(
                            new LogInResponseDTO(),
                            result.Message,
                            500,
                            result.Errors)),

                    _ => StatusCode(
                        result.StatusCode,
                        Response<LogInResponseDTO>.Failure(
                            new LogInResponseDTO(),
                            result.Message,
                            result.StatusCode))
                };
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning(
                    "Login request was cancelled");

                return StatusCode(
                    499,
                    Response<LogInResponseDTO>.Failure(
                        new LogInResponseDTO(),
                        "Request was cancelled",
                        499));
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unexpected error during login");

                return StatusCode(
                    500,
                    Response<LogInResponseDTO>.Failure(
                        new LogInResponseDTO(),
                        "An unexpected error occurred. Please try again later.",
                        500,
                        [ex.Message]));
            }
        }

        [HttpPost("google-login")]
        public async Task<ActionResult<Response<LogInResponseDTO>>> GoogleLogin([FromBody]GoogleExchangeRequest request)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"Validation failed",400,validationErrors));
            }

            if (string.IsNullOrWhiteSpace(request.IdToken))
                return BadRequest(Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"ID token is required",400));

            try
            {
                var result = await googleAuthService.GoogleLogin(request.IdToken, request.Platform ?? "web");

                return result.StatusCode switch
                {
                    200 => Ok(Response<LogInResponseDTO>.Success(result.Data!,result.Message,200)),

                    401 => Unauthorized(Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),result.Message,401)),

                    400 => BadRequest(Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),result.Message,400,result.Errors)),

                    500 => StatusCode(500, Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),result.Message,500,result.Errors)),

                    _ => StatusCode(result.StatusCode,Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),result.Message,result.StatusCode))
                };
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Google login request was cancelled");
                return StatusCode(499, Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"Request was cancelled",499));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during Google login");
                return StatusCode(500, Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"An unexpected error occurred. Please try again later.",500,[ex.Message]));
            }
        }
    }

}


