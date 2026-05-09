using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Profile;
using Requra.Application.Interfaces.IProfileService;
using Requra.Application.Response;
using Requra.Infrastructure.Services.ProfileService;
using System.Security.Claims;

namespace Requra.Presentation.Controllers.Profile
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController(IProfileService profileService) : ControllerBase
    {
        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarDto uploadAvatar, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return StatusCode(401, Response<UploadAvatarResponse>.Failure(new(), "User not authenticated", 401));
            }

            var result = await profileService.UploadAvatarAsync(uploadAvatar, userId, cancellationToken);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return StatusCode(401, Response<ProfileDto>.Failure(new(), "User not authenticated", 401));
            }


            var profile = await profileService.GetProfileAsync(userId);
            return StatusCode(profile.StatusCode, profile);
        }
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return StatusCode(401, Response<ProfileDto>.Failure(new(), "User not authenticated", 401));

            }

            var updateProfile = await profileService.UpdateNameAsync(userId, request);
            return StatusCode(updateProfile.StatusCode, updateProfile);



        }
        [HttpDelete]
        public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return StatusCode(401, Response<string>.Failure(string.Empty, "User not authenticated", 401));
            }

            var result = await profileService.DeleteAccountAsync(userId, cancellationToken);

            return StatusCode(result.StatusCode, result);
        }

    }
}