using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Requra.Application.DTOs.Auth.Login;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.ExternalDTOs.ExternalAuth.GoogleAuthDTO;
using Requra.Infrastructure.ExternalInterfaces.IExternalAuth;
using Requra.Infrastructure.ExternalInterfaces.IJwtTokenService;
using Requra.Infrastructure.Services.AuthService;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Requra.Infrastructure.ExternalServices.ExternalAuth
{
    public class GoogleAuthService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, IAuthService authService, IHttpContextAccessor httpContextAccessor) : IGoogleAuthService
    {
        public async Task<Response<LogInResponseDTO>> GoogleLogin(string googleToken, string platform = "web")
        {
            var payload = await VerifyGoogleToken(googleToken);
            if (payload == null)
            {
                return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"UnAuthorized User",401);
            }

            if (!payload.EmailVerified)
            {
                return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"Google email is not verified",401);
            }

            using var scope = serviceScopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

            var user = await userManager.FindByEmailAsync(payload.Email);
            var isNewUser = user == null;

            if (user == null)
            {
                string fullName = $"{payload.GivenName} {payload.FamilyName}".Trim();
                if (string.IsNullOrEmpty(fullName))
                    fullName = payload.Email.Split('@')[0];

                user = new ApplicationUser(payload.Email, payload.Email, fullName, Language.En, payload.Picture)
                {
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"Failed to create user account",500,createResult.Errors.Select(e => e.Description).ToList());
                }

                var existingRoles = await userManager.GetRolesAsync(user);
                if (!existingRoles.Any())
                {
                    await userManager.AddToRoleAsync(user, "Stakeholder");
                }
            }
            else
            {
                bool needsUpdate = false;

                if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(payload.Picture))
                {
                    user.UpdateProfile(user.FullName, user.PreferredLanguage, payload.Picture);
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    await userManager.UpdateAsync(user);
                }

                // Keep existing roles unchanged.
                var existingRoles = await userManager.GetRolesAsync(user);

                // Optional fallback if user somehow has no identity role at all
                if (!existingRoles.Any())
                {
                    await userManager.AddToRoleAsync(user, "Stakeholder");
                }
            }

            var userRoles = await userManager.GetRolesAsync(user);
            var jwtToken = await jwtService.GenerateJwtToken(user);
            var generatedToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            var tokenExpiry = jwtToken.ValidTo;

            var refreshToken = await authService.GetOrCreateRefreshToken(user);
            SetRefreshTokenCookie(refreshToken.Token, platform);

            var userData = new LogInResponseDTO()
            {
                UserId = user.Id,
                Name = user.FullName,
                ProfilePicture = user.AvatarUrl,
                Roles = userRoles.ToList(),
                IsAuthenticated = true,
                Token = generatedToken,
                TokenExpiry = tokenExpiry,
                RefreshToken = platform is "android" or "ios"
                    ? refreshToken.Token
                    : string.Empty,
                IsNewUser = isNewUser
            };

            return Response<LogInResponseDTO>.Success(userData, "Login Successfully", 200);
        }
        private async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string token)
        {

            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new[] { configuration["GoogleAuth:WebClientId"] ,
                                   configuration["GoogleAuth:AndroidClientId"],
                                   configuration["GoogleAuth:IosClientId"]},
                IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
                ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5)
            };

            return await GoogleJsonWebSignature.ValidateAsync(token, settings);
        }
        private void SetRefreshTokenCookie(string refreshToken, string platform)
        {
            if (platform is "android" or "ios")
                return;

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
        private static async Task EnsureUserHasAnyRoleAsync(UserManager<ApplicationUser> userManager,ApplicationUser user,string defaultRoleName)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Any())
            {
                await userManager.AddToRoleAsync(user, defaultRoleName);
            }
        }

    }
}
