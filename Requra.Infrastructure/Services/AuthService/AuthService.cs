using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Requra.Application.DTOs.Auth.Login;
using Requra.Application.DTOs.Auth.Otp;
using Requra.Application.DTOs.Auth.RefreshToken;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Interfaces.IOtpService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.ExternalInterfaces.IJwtTokenService;
using Requra.Infrastructure.Helpers;
using Requra.Infrastructure.Services.JWTService;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;

namespace Requra.Infrastructure.Services.AuthService
{

    public class AuthService(UserManager<ApplicationUser> userManager, IValidator<RegisterRequestDto> validator, IValidator<RefreshTokenRequestDto> refreshTokenValidator, IJwtTokenService _jwtService, IConfiguration config, IServiceScopeFactory serviceScopeFactory, IHttpContextAccessor httpContextAccessor, IOtpService otpService) : IAuthService
    {
        public async Task<Response<string>> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
                //Validation Handeled Here only For Now due To Problem In Auto Validation.

                var validation = await validator.ValidateAsync(request);

                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                    return Response<string>.Failure("", "Validation failed", 400, errors);
                }

                //needs refactoring later
                var existingUser = await userManager.FindByEmailAsync(request.Email);
                if (existingUser != null && existingUser.IsActive)  //Instead of Reregistering the user, we can REACTIVATE the user later
                {
                    return Response<string>.Failure("",
                        "Email already exists",
                        400,
                        ["A user with this email is already registered."]
                    );
                }
                var user = new ApplicationUser(request.Email, request.Email, request.FullName, Language.En)
                {
                    Role = request.Role
                };

                var result = await userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    return Response<string>.Failure("",
                        "Validation failed",
                        400,
                        result.Errors.Select(e => e.Description).ToList()
                    );
                }

                var roleNames = RoleHelper.GetRoleNames(request.Role);


                var roleResult = await userManager.AddToRolesAsync(user, roleNames);

                if (!roleResult.Succeeded)
                {
                    await userManager.DeleteAsync(user);
                    return Response<string>.Failure(
                        "",
                        "Failed to assign role",
                        400,
                        roleResult.Errors.Select(e => e.Description).ToList()
                    );
                }

                //---------Needed Only When Debugging Refresh Token Endpoint Until Login Endpoint Created------
                var newAccessToken = await _jwtService.GenerateTokenAsync(user);
                var newRefreshToken = await _jwtService.GenerateRefreshToken();
                var refreshTokenDays = config.GetValue<int>("JWT:RefreshTokenDurationInDays");

                user.RefreshTokens.Add(new RefreshToken
                {
                    Token = newRefreshToken,
                    CreatedOn = DateTime.UtcNow,
                    ExpiresOn = DateTime.UtcNow.AddDays(refreshTokenDays)
                });
                var updateResult = await userManager.UpdateAsync(user);

                Console.WriteLine(newRefreshToken);
                Console.WriteLine(newAccessToken);
                ////---------------------------------------

                // await _emailService.SendOtpAsync(user.Email);
                await otpService.GenerateAndSendAsync(user, OtpPurpose.EmailConfirmation);

                return Response<string>.Success("Done successfully", "User registered successfully", 201);
            }
            catch (Exception ex)
            {
                return Response<string>.Failure(
                    "",
                    $"An unexpected error occurred.",
                    500,
                    []
                );
            }
        }

        public async Task<Response<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
        {

            try
            {
                //Validation Handeled Here only For Now due To Problem In Auto Validation.
                var validation = await refreshTokenValidator.ValidateAsync(request);

                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(), "Validation failed", 400, errors);
                }


                ClaimsPrincipal principal;
                try
                {
                    principal = await _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
                }
                catch (Exception ex)
                {
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(), "Invalid access token", 401);
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(), "Invalid access token", 401);

                var user = await userManager.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || !user.IsActive)
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(), "User not found", 404);

                var storedToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token.Trim() == request.RefreshToken.Trim());

                if (storedToken == null || !storedToken.IsActive)
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(), "Invalid refresh token", 401);

                storedToken.RevokedOn = DateTime.UtcNow;
                var newAccessToken = await _jwtService.GenerateTokenAsync(user);
                var newRefreshToken = await _jwtService.GenerateRefreshToken();
                var refreshTokenDays = config.GetValue<int>("JWT:RefreshTokenDurationInDays");

                user.RefreshTokens.Add(new RefreshToken
                {
                    Token = newRefreshToken,
                    CreatedOn = DateTime.UtcNow,
                    ExpiresOn = DateTime.UtcNow.AddDays(refreshTokenDays)
                });
                var updateResult = await userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    return Response<RefreshTokenResponseDto>.Failure(
                        "Failed to update user refresh token",
                        500,
                        []
                    );
                }

                var roles = await userManager.GetRolesAsync(user);

                var data = new RefreshTokenResponseDto
                {
                    UserId = user.Id,
                    Name = user.UserName,
                    IsAuthenticated = true,
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Roles = roles.ToList(),
                    ProfilePicture = user.AvatarUrl
                };

                return Response<RefreshTokenResponseDto>.Success(data, "Token refreshed successfully", 200);
            }
            catch (Exception)
            {
                return Response<RefreshTokenResponseDto>.Failure($"An unexpected error occurred.", 500, []);
            }
        }


        public async Task<RefreshToken> GetOrCreateRefreshToken(ApplicationUser user)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

            var userFromDb = await userManager.FindByIdAsync(user.Id);

            if (userFromDb == null)
                throw new Exception("User not found in database");

            var activeToken = userFromDb.RefreshTokens.FirstOrDefault(t => t.IsActive);
            if (activeToken != null)
                return activeToken;

            var newRefreshToken = jwtService.CreateRefreshToken();

            userFromDb.RefreshTokens.Add(newRefreshToken);
            await userManager.UpdateAsync(userFromDb);

            return newRefreshToken;
        }

        public async Task<RefreshToken> CreateRefreshTokenForLogin(ApplicationUser user, ClientPlatform platform = ClientPlatform.Web)
        {
            var refreshToken =
                _jwtService.CreateRefreshToken();

            //refreshToken.Platform =
            //    platform.ToString();

            user.RefreshTokens.Add(refreshToken);

            var result =
                await userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new Exception(
                    "Unable to save refresh token");

            return refreshToken;
        }


        public async Task<Response<string>> LogoutAsync(string userId)
        {

            try
            {
                var user = await userManager.Users
                 .Include(u => u.RefreshTokens)
                 .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || !user.IsActive)
                    return Response<string>.Failure("", "User not found", 404);

                foreach (var token in user.RefreshTokens)
                {
                    token.RevokedOn = DateTime.UtcNow;
                }

                await userManager.UpdateAsync(user);

                return Response<string>.Success("Done", "Logged out successfully", 200);
            }
            catch (Exception)
            {
                return Response<string>.Failure("", $"An unexpected error occurred.", 500, []);
            }

        }

        public async Task<Response<LogInResponseDTO>> LoginAsync(LoginRequestDto request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(), "Invalid credentials", 401);
            }

            var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);

            if (!passwordValid)
            {
                return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(), "Invalid credentials", 401);
            }

            //after Carol Part for confirm email
            //if (!user.EmailConfirmed)
            //{
            //    return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"Email not confirmed",403);
            //}
            if (!user.EmailConfirmed)
                return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(), "Please confirm your email before logging in.", 403);

            var jwt = await _jwtService.GenerateJwtToken(user);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            var refreshToken = await CreateRefreshTokenForLogin(user, request.Platform);

            if (request.Platform == ClientPlatform.Web)
            {
                SetRefreshTokenCookie(refreshToken.Token);
            }

            var roles = await userManager.GetRolesAsync(user);

            return Response<LogInResponseDTO>
                .Success(
                    new LogInResponseDTO
                    {
                        UserId = user.Id,
                        Name = user.FullName,
                        ProfilePicture = user.AvatarUrl,
                        Roles = roles.ToList(),
                        Token = accessToken,
                        TokenExpiry = jwt.ValidTo,
                        IsAuthenticated = true,

                        RefreshToken =
                            request.Platform ==
                            ClientPlatform.Web
                            ? string.Empty
                            : refreshToken.Token
                    },
                    "Login successful",
                    200);
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };

            httpContextAccessor.HttpContext!.Response.Cookies
                .Append("secure_rtk", refreshToken, cookieOptions);
        }
        public async Task<Response<string>> ConfirmAccountAsync(ConfirmAccountRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
                return Response<string>.Failure("", "Email and code are required.", 400);

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Response<string>.Failure("", "Invalid email or code.", 400);

            if (user.EmailConfirmed)
                return Response<string>.Success("Account already confirmed", "Account is already confirmed.", 200);

            var otpResult = await otpService.VerifyAsync(user, request.Code, OtpPurpose.EmailConfirmation);
            if (!otpResult.IsSuccess)
                return Response<string>.Failure("", otpResult.Message, otpResult.StatusCode);

            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);

            return Response<string>.Success("Account confirmed", "Account confirmed successfully.", 200);
        }

        public async Task<Response<bool>> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Response<bool>.Failure(false, "Email is required.", 400);

            var user = await userManager.FindByEmailAsync(request.Email);

            // Always return the same success shape regardless of whether the email exists —
            // prevents attackers from using this endpoint to discover registered emails.
            if (user != null && user.IsActive)
                await otpService.GenerateAndSendAsync(user, OtpPurpose.PasswordReset);

            return Response<bool>.Success(true, "If an account exists with this email, a reset code has been sent.", 200);
        }

        public async Task<Response<bool>> ResendOtpAsync(ResendOtpRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Response<bool>.Failure(false, "Email is required.", 400);

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Response<bool>.Success(true, "If an account exists with this email, a new code has been sent.", 200);

            if (request.Purpose == OtpPurpose.EmailConfirmation && user.EmailConfirmed)
                return Response<bool>.Failure(false, "Account is already confirmed.", 400);

            return await otpService.ResendAsync(user, request.Purpose);
        }

        public async Task<Response<bool>> VerifyOtpAsync(VerifyOtpRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
                return Response<bool>.Failure(false, "Email and code are required.", 400);

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Response<bool>.Failure(false, "Invalid email or code.", 400);

            // Read-only check — does NOT consume the code. Safe to call for live validation in the UI.
            return await otpService.CheckAsync(user, request.Code, OtpPurpose.PasswordReset);
        }

        public async Task<Response<bool>> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
                return Response<bool>.Failure(false, "Email and code are required.", 400);

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword != request.ConfirmNewPassword)
                return Response<bool>.Failure(false, "Passwords do not match.", 400);

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Response<bool>.Failure(false, "Invalid request.", 400);

            // Real check — consumes the code so it can't be reused.
            var otpResult = await otpService.VerifyAsync(user, request.Code, OtpPurpose.PasswordReset);
            if (!otpResult.IsSuccess)
                return Response<bool>.Failure(false, otpResult.Message, otpResult.StatusCode);

            await userManager.RemovePasswordAsync(user);
            var addResult = await userManager.AddPasswordAsync(user, request.NewPassword);

            if (!addResult.Succeeded)
                return Response<bool>.Failure(false, "Failed to reset password", 400, addResult.Errors.Select(e => e.Description).ToList());

            // Force re-login on every device after a password change.
            foreach (var token in user.RefreshTokens ?? [])
                token.RevokedOn = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            return Response<bool>.Success(true, "Password reset successfully.", 200);
        }




        public async Task<Response<bool>> ChangeRoleAsync(ChangeUserRoleRequestDto request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.UserId))
                errors.Add("UserId is required.");

            if (!Enum.IsDefined(typeof(UserRole), request.Role))
                errors.Add("Role is invalid.");

            if (errors.Any())
            {
                return Response<bool>.Failure(false, "Validation failed.", 400, errors);
            }

            try
            {
                var user = await userManager.Users
                    .FirstOrDefaultAsync(x => x.Id == request.UserId);

                if (user == null)
                {
                    return Response<bool>.Failure(false, "User not found.", 404);
                }

                // Update custom enum role property
                user.Role = request.Role;

                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return Response<bool>.Failure(false, "Failed to update user role.", 500, updateResult.Errors.Select(x => x.Description).ToList());

                }

                // Sync identity roles
                var currentIdentityRoles = await userManager.GetRolesAsync(user);
                if (currentIdentityRoles.Any())
                {
                    var removeResult = await userManager.RemoveFromRolesAsync(user, currentIdentityRoles);
                    if (!removeResult.Succeeded)
                    {
                        return Response<bool>.Failure(false, "Failed to remove existing identity roles.", 500, removeResult.Errors.Select(x => x.Description).ToList());
                    }
                }

                var newRoleNames = RoleHelper.GetRoleNames(request.Role);

                if (newRoleNames.Any())
                {
                    var addResult = await userManager.AddToRolesAsync(user, newRoleNames);
                    if (!addResult.Succeeded)
                    {
                        return Response<bool>.Failure(false, "Failed to assign new identity roles.", 500, addResult.Errors.Select(x => x.Description).ToList());
                    }
                }

                var finalRoles = await userManager.GetRolesAsync(user);

                return Response<bool>.Success(true, "User role changed successfully.", 200);
            }
            catch (Exception ex)
            {
                return Response<bool>.Failure(false, "An unexpected error occurred while changing user role.", 500, new List<string> { ex.Message });
            }
        }
    }
}