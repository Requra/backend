using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Requra.Application.DTOs.Profile;
using Requra.Application.Interfaces.IProfileService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Services.ProfileService
{
    public class ProfileService(UserManager<ApplicationUser> _userManager, ICloudinaryService _cloudinaryService, RequraDbContext _context) : IProfileService
    {
        public async Task<Response<string>> UploadAvatarAsync(UploadAvatarDto uploadAvatar,string userId,CancellationToken cancellationToken = default)
        {
            if (uploadAvatar == null || uploadAvatar.File == null || uploadAvatar.File.Length == 0)
                return Response<string>.Failure("", "File is required", 400);

            if (string.IsNullOrEmpty(userId))
                return Response<string>.Failure("", "Invalid user", 400);

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };

            if (!allowedTypes.Contains(uploadAvatar.File.ContentType))
                return Response<string>.Failure("", "Invalid file type", 400);

            if (uploadAvatar.File.Length > 5 * 1024 * 1024)
                return Response<string>.Failure("", "File exceeds 5MB", 400);

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return Response<string>.Failure("", "User not found", 404);

                var folder = $"users/{userId}/avatar";

                var publicId = $"{userId}_avatar";

                var uploadResult = await _cloudinaryService.UploadFileAsync(
                    uploadAvatar.File,
                    folder, 
                    publicId: publicId,
                    overwrite: true,
                    cancellationToken: cancellationToken
                );

                // only store URL
                user.UpdateAvatar(uploadResult.Url);
                await _userManager.UpdateAsync(user);

                await transaction.CommitAsync(cancellationToken);

                return Response<string>.Success(
                    uploadResult.Url,
                    "Avatar uploaded successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Response<string>.Failure(
                    "",
                    $"Error uploading avatar: {ex.Message}",
                    500
                );
            }
        }
    }
}
