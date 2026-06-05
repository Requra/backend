using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Requra.Application.DTOs.Auth.Login;
using Requra.Application.DTOs.Auth.RefreshToken;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.Interfaces.IAuthService;
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

    public class AuthService(UserManager<ApplicationUser> userManager, IValidator<RegisterRequestDto> validator, IValidator<RefreshTokenRequestDto> refreshTokenValidator, IJwtTokenService _jwtService, IConfiguration config, IServiceScopeFactory serviceScopeFactory, IHttpContextAccessor httpContextAccessor) : IAuthService
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
                    return Response<string>.Failure("","Validation failed", 400, errors);
                }

                //needs refactoring later
                var existingUser = await userManager.FindByEmailAsync(request.Email);
                if (existingUser is not null)
                {
                    return Response<string>.Failure("",
                        "Email already exists",
                        400,
                        ["A user with this email is already registered."]
                    );
                }
                var user = new ApplicationUser(request.Email, request.Email)
                {
                    Role = request.Role
                };
                user.UpdateProfile(request.FullName, null, null);

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

                //Console.WriteLine(newRefreshToken);
                //Console.WriteLine(newAccessToken);
                ////---------------------------------------
                Console.WriteLine(newRefreshToken);
                Console.WriteLine(newAccessToken);
                ////---------------------------------------

                // await _emailService.SendOtpAsync(user.Email);        

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
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto() , "Invalid access token", 401); 
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                   return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(),"Invalid access token", 401);
                
                var user = await userManager.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(),"User not found", 404);

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
                return Response<RefreshTokenResponseDto>.Failure($"An unexpected error occurred.",500,[]);
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

            var newRefreshToken =  jwtService.CreateRefreshToken();

            userFromDb.RefreshTokens.Add(newRefreshToken);
            await userManager.UpdateAsync(userFromDb);

            return newRefreshToken;
        }

        public async Task<RefreshToken> CreateRefreshTokenForLogin(ApplicationUser user,ClientPlatform platform=ClientPlatform.Web)
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

                if (user == null)
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
            var user =await userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(), "Invalid credentials", 401);
            }

            var passwordValid = await userManager.CheckPasswordAsync( user, request.Password);

            if (!passwordValid)
            {
                return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"Invalid credentials",401);
            }

            //after Carol Part for confirm email
            //if (!user.EmailConfirmed)
            //{
            //    return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"Email not confirmed",403);
            //}

            var jwt =await _jwtService.GenerateJwtToken(user);

            var accessToken =new JwtSecurityTokenHandler().WriteToken(jwt);

            var refreshToken =await CreateRefreshTokenForLogin(user,request.Platform);

            if (request.Platform == ClientPlatform.Web)
            {
                SetRefreshTokenCookie(refreshToken.Token);
            }

            var roles =await userManager.GetRolesAsync(user);

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
    }
}