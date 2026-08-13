using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs.Auth.Otp;
using Requra.Application.DTOs.Profile;
using Requra.Application.Interfaces.IProfileService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;
using Requra.Infrastructure.Services.OtpService;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Requra.Infrastructure.Services.ProfileService
{
    public class ProfileService(UserManager<ApplicationUser> _userManager, ICloudinaryService _cloudinaryService, RequraDbContext _context, IValidator<UploadAvatarDto> validator,IValidator<UpdateProfileDto> updateProfileValidator, ILogger<ProfileService> logger) : IProfileService
    {
        public async Task<Response<UploadAvatarResponse>> UploadAvatarAsync(UploadAvatarDto uploadAvatar, string userId, CancellationToken cancellationToken = default)
        {
            var validation = await validator.ValidateAsync(uploadAvatar);

            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return Response<UploadAvatarResponse>.Failure(new(), "Validation failed", 400, errors);
            }
            if (uploadAvatar == null || uploadAvatar.File == null || uploadAvatar.File.Length == 0)
                return Response<UploadAvatarResponse>.Failure(new(), "File is required", 400);

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null || !user.IsActive)
                    return Response<UploadAvatarResponse>.Failure(new UploadAvatarResponse(), "User not found", 404);

                var folder = $"users/{userId}/avatar";

                var publicId = $"{userId}_avatar";

                var uploadResult = await _cloudinaryService.UploadFileAsync(
                    uploadAvatar.File,
                    folder,
                    publicId: publicId,
                    overwrite: true,
                    cancellationToken: cancellationToken
                );

                user.UpdateAvatar(uploadResult.Url);
                await _userManager.UpdateAsync(user);

                await transaction.CommitAsync(cancellationToken);

                return Response<UploadAvatarResponse>.Success(
                    new UploadAvatarResponse { AvatarUrl = uploadResult.Url },
                    "Avatar uploaded successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
                return Response<UploadAvatarResponse>.Failure(
                    new UploadAvatarResponse(),
                    $"Error uploading avatar",
                    500
                );
            }
        }

        public async Task<Response<ProfileDto>> GetProfileAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return Response<ProfileDto>.Failure(new ProfileDto(), "User not found", 404);


                return Response<ProfileDto>.Success(
                    new ProfileDto
                    {
                        Id = user.Id,
                        Name = user.FullName,
                        Email = user.Email,
                        JobTitle = user.Role,
                        AvatarUrl = user.AvatarUrl,
                        CreatedAt = user.CreatedAt
                    },
                    "Profile fetched successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching profile for user {UserId}", userId);
                return Response<ProfileDto>.Failure(new ProfileDto(), "Error fetching profile", 500);

            }
        }
        public async Task<Response<ProfileDto>> UpdateNameAsync(string userId, UpdateProfileDto updateProfile)
        {
            var validation = await updateProfileValidator.ValidateAsync(updateProfile);

            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return Response<ProfileDto>.Failure(new(), "Validation failed", 400, errors);
            }
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || !user.IsActive)
                return Response<ProfileDto>.Failure(new ProfileDto(), "User not found", 404);

            user.UpdateProfile(updateProfile.Name);

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {

                logger.LogError(string.Join(", ", result.Errors.Select(e => e.Description)), "Error updating profile for user {UserId}: {Errors}", userId);
                return Response<ProfileDto>.Failure(new ProfileDto(), "Error updating profile", 500);
            }

            return Response<ProfileDto>.Success(new ProfileDto
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email,
                JobTitle = user.Role,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt
            }, "Profile updated successfully");
        }

        public async Task<Response<string>> DeleteAccountAsync(string userId, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null || !user.IsActive)
                    return Response<string>.Failure(string.Empty, "User not found", 404);

                user.Deactivate();

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    logger.LogError(
                        "Error deleting account for user {UserId}: {Errors}",
                        userId,
                        string.Join(", ", result.Errors.Select(e => e.Description))
                    );

                    return Response<string>.Failure(string.Empty, "Error deleting account", 500);
                }

                await transaction.CommitAsync(cancellationToken);

                return Response<string>.Success(
                    string.Empty,
                    "Account deleted successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                logger.LogError(ex, "Exception while deleting account for user {UserId}", userId);

                return Response<string>.Failure(string.Empty, "Error deleting account", 500);
            }
        }

        public async Task<Response<bool>> ChangePasswordAsync(ChangePasswordRequestDto request,CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request == null)
            {
                return Response<bool>.Failure(false, "Request is required.", StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                errors.Add("CurrentPassword is required.");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                errors.Add("NewPassword is required.");
            else if (request.NewPassword.Length < 8)
                errors.Add("NewPassword must be at least 8 characters long.");

            if (!string.IsNullOrWhiteSpace(request.CurrentPassword) &&
                !string.IsNullOrWhiteSpace(request.NewPassword) &&
                request.CurrentPassword == request.NewPassword)
            {
                errors.Add("NewPassword must be different from CurrentPassword.");
            }

            if (errors.Any())
            {
                return Response<bool>.Failure(false, "Validation failed.", StatusCodes.Status400BadRequest, errors);
            }

            var user = await _userManager.FindByIdAsync(request.CurrentUserId);
            if (user == null)
            {
                return Response<bool>.Failure(false, "User not found.", StatusCodes.Status404NotFound);
            }

            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                return Response<bool>.Failure(false, "Current password is incorrect.", StatusCodes.Status400BadRequest);
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user,request.CurrentPassword,request.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                return Response<bool>.Failure(false,"Failed to change password.",StatusCodes.Status400BadRequest,changePasswordResult.Errors.Select(e => e.Description).ToList());
            }

            if (user.RefreshTokens != null)
            {
                foreach (var token in user.RefreshTokens.Where(x => x.RevokedOn == null))
                {
                    token.RevokedOn = DateTime.UtcNow;
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return Response<bool>.Failure(false,"Password changed, but failed to update user session state.",StatusCodes.Status500InternalServerError,updateResult.Errors.Select(e => e.Description).ToList());
            }

            return Response<bool>.Success(true, "Password changed successfully.", StatusCodes.Status200OK);
        }
    }
}