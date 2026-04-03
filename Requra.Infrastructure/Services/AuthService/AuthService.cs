using Microsoft.AspNetCore.Identity;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;

namespace Requra.Infrastructure.Services.AuthService
{
    public class AuthService(UserManager<ApplicationUser> userManager) : IAuthService
    {
        public async Task<Response<string>> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
                var existingUser = await userManager.FindByEmailAsync(request.Email);
                if (existingUser is not null)
                {
                    return Response<string>.Failure(
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
                    return Response<string>.Failure(
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
                    return Response<string>.Failure("Invalid role", 400, [$"This Role is not supported."]);
                }

                var roleResult = await userManager.AddToRoleAsync(user, roleName);

                if (!roleResult.Succeeded)
                {
                    await userManager.DeleteAsync(user);
                    return Response<string>.Failure(
                        "Failed to assign role",
                        400,
                        roleResult.Errors.Select(e => e.Description).ToList()
                    );
                }

                // await _emailService.SendOtpAsync(user.Email);

                return Response<string>.Success("Done successfully", "User registered successfully", 200);
            }
            catch (Exception ex)
            {
                return Response<string>.Failure(
                    $"An unexpected error occurred. Please try again later.\n {ex.Message}",
                    500,
                    []
                );
            }
        }
    }
}