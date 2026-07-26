using Microsoft.AspNetCore.Http;
using Requra.Application.DTOs.Profile;
using Requra.Application.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IProfileService
{
    public interface IProfileService
    {
        public  Task<Response<UploadAvatarResponse>> UploadAvatarAsync(UploadAvatarDto uploadAvatar, string userId, CancellationToken cancellationToken = default);
        Task<Response<ProfileDto>> GetProfileAsync(string userId);
        Task<Response<ProfileDto>> UpdateNameAsync(string userId, UpdateProfileDto updateProfile );
        Task<Response<string>> DeleteAccountAsync(string userId, CancellationToken cancellationToken = default);
        Task<Response<bool>> ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken cancellationToken = default);

    }
}
