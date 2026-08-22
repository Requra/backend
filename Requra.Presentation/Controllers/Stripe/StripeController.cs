using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.Response;
using Requra.Domain.Enums;
using Requra.Infrastructure.ExternalInterfaces.IPaymentService.StripeService;
using System.Security.Claims;

namespace Requra.Presentation.Controllers.Stripe
{
    [ApiController]
    [Route("api/stripe")]
    public class StripeController : ControllerBase
    {
        private readonly IStripeBillingService _stripeBillingService;

        public StripeController(IStripeBillingService stripeBillingService)
        {
            _stripeBillingService = stripeBillingService;
        }

        [Authorize]
        [HttpPost("checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession(
            [FromBody] CreateCheckoutSessionRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
                return BadRequest(Response<string>.Failure("Request body is required.", 400));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(Response<string>.Failure("Unauthorized.", 401));

            var response = await _stripeBillingService.CreateCheckoutSessionAsync(
                userId,
                request.Interval,
                cancellationToken);

            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpPost("customer-portal")]
        public async Task<IActionResult> CreateCustomerPortalSession(
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(Response<string>.Failure("Unauthorized.", 401));

            var response = await _stripeBillingService.CreateCustomerPortalSessionAsync(
                userId,
                cancellationToken);

            return StatusCode(response.StatusCode, response);
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(HttpContext.Request.Body);
            var json = await reader.ReadToEndAsync(cancellationToken);
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

            var response = await _stripeBillingService.HandleWebhookAsync(
                json,
                stripeSignature,
                cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
    }

    public sealed class CreateCheckoutSessionRequest
    {
        public BillingInterval Interval { get; set; }
    }
}
