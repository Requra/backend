using Requra.Application.Response;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalInterfaces.IPaymentService.StripeService
{
    public interface IStripeBillingService
    {
        Task<Response<string>> CreateCheckoutSessionAsync(string userId,BillingInterval interval,CancellationToken cancellationToken = default);

        Task<Response<string>> CreateCustomerPortalSessionAsync(string userId,CancellationToken cancellationToken = default);

        Task<Response<bool>> HandleWebhookAsync(string json,string stripeSignature,CancellationToken cancellationToken = default);
    }
}
