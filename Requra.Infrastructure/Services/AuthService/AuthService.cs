using Microsoft.AspNetCore.Identity;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Interfaces.IJwtTokenService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;

namespace Requra.Infrastructure.Services.AuthService
{
    public class AuthService(UserManager<ApplicationUser> userManager) : IAuthService
    {
        public async Task<Response<object>> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
                var user = new ApplicationUser(request.Email, request.Email)
                {
                    Role = request.Role
                };

                user.UpdateProfile(request.FullName, null, null);

                var result = await userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    return Response<object>.Failure(
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
                    return Response<object>.Failure("Invalid role", 400, [$"This Role is not supported."]);
                }

                var roleResult = await userManager.AddToRoleAsync(user, roleName);

                if (!roleResult.Succeeded)
                {
                    await userManager.DeleteAsync(user);
                    return Response<object>.Failure(
                        "Failed to assign role",
                        400,
                        roleResult.Errors.Select(e => e.Description).ToList()
                    );
                }

                // await _emailService.SendOtpAsync(user.Email);

                return Response<object>.Success(null, "User registered successfully", 200);
            }
            catch (Exception)
            {
                return Response<object>.Failure(
                    "An unexpected error occurred. Please try again later.",
                    500,
                    []
                );
            }
        }
    }
}