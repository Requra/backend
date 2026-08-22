using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.UserSubscription;
using Requra.Application.Interfaces.IUserSubscriptionService;
using Requra.Application.Response;
using System.Security.Claims;

namespace Requra.Presentation.Controllers.Stripe
{
    [ApiController]
    [Route("api/subscriptions")]
    [Authorize]
    public class UserSubscriptionController : ControllerBase
    {
        private readonly IUserSubscriptionService _userSubscriptionService;

        public UserSubscriptionController(IUserSubscriptionService userSubscriptionService)
        {
            _userSubscriptionService = userSubscriptionService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMySubscription(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(Response<UserSubscriptionDto>.Failure("Unauthorized.", 401));

            var response = await _userSubscriptionService.GetByUserIdAsync(userId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("me/ensure")]
        public async Task<IActionResult> EnsureMySubscription(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(Response<UserSubscriptionDto>.Failure("Unauthorized.", 401));

            var response = await _userSubscriptionService.EnsureExistsAsync(userId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("me/starter")]
        public async Task<IActionResult> SetStarter(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(Response<UserSubscriptionDto>.Failure("Unauthorized.", 401));

            var response = await _userSubscriptionService.SetStarterAsync(userId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("me/has-professional-access")]
        public async Task<IActionResult> HasProfessionalAccess(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(Response<bool>.Failure("Unauthorized.", 401));

            var response = await _userSubscriptionService.HasProfessionalAccessAsync(userId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("me/can-create-project")]
        public async Task<IActionResult> CanCreateProject(
            [FromQuery] int currentProjectCountThisMonth,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(Response<bool>.Failure("Unauthorized.", 401));

            var response = await _userSubscriptionService.CanCreateProjectAsync(
                userId,
                currentProjectCountThisMonth,
                cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
    }
}
