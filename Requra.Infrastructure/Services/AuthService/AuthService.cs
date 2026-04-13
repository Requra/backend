using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Requra.Application.DTOs.Auth.RefreshToken;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.ExternalInterfaces.IJwtTokenService;
using System.Security.Claims;
using System.Security.Principal;

namespace Requra.Infrastructure.Services.AuthService
{
    public class AuthService(UserManager<ApplicationUser> userManager, IJwtTokenService _jwtService, IConfiguration config) : IAuthService
    {
        public async Task<Response<string>> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
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

                var roleName = request.Role switch
                {
                    UserRole.ProjectManager => "ProjectManager",
                    UserRole.BusinessAnalyst => "BusinessAnalyst",
                    UserRole.Stakeholder => "Stakeholder",
                    _ => null
                };


                if (roleName is null)
                {
                    await userManager.DeleteAsync(user);
                    return Response<string>.Failure("", "Invalid role", 400, [$"This Role is not supported."]);
                }

                var roleResult = await userManager.AddToRoleAsync(user, roleName);

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
                //var newAccessToken = await _jwtService.GenerateTokenAsync(user);
                //var newRefreshToken = await _jwtService.GenerateRefreshToken();
                //var refreshTokenDays = config.GetValue<int>("JWT:RefreshTokenDurationInDays");

                //user.RefreshTokens.Add(new RefreshToken
                //{
                //    Token = newRefreshToken,
                //    CreatedOn = DateTime.UtcNow,
                //    ExpiresOn = DateTime.UtcNow.AddDays(refreshTokenDays)
                //});
                //var updateResult = await userManager.UpdateAsync(user);

                //Console.WriteLine(newRefreshToken);
                //Console.WriteLine(newAccessToken);
                //---------------------------------------

                // await _emailService.SendOtpAsync(user.Email);        

                return Response<string>.Success("Done successfully", "User registered successfully", 201);
            }
            catch (Exception)
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
    }
}