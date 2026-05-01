using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs.Profile;
using Requra.Application.Interfaces.IProfileService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;
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

                if (user == null)
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

            if (user == null)
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
    }
}