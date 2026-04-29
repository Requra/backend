using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Profile;
using Requra.Application.Interfaces.IProfileService;
using Requra.Application.Response;
using System.Security.Claims;

namespace Requra.Presentation.Controllers.Profile
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController(IProfileService profileService) : ControllerBase
    {
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarDto uploadAvatar, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (uploadAvatar == null|| uploadAvatar.File == null || uploadAvatar.File.Length == 0)
                return BadRequest(Response<string>.Failure("File is required", 400));
            var result = await profileService.UploadAvatarAsync(uploadAvatar, userId!, cancellationToken);

         
            return StatusCode(result.StatusCode, result);
        }
    }
}
