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

            var result = await profileService.UploadAvatarAsync(uploadAvatar, userId!, cancellationToken);

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
    }
}