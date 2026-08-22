using Requra.Application.DTOs.UserSubscription;
using Requra.Application.Response;
using Stripe;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Requra.Application.Interfaces.IUserSubscriptionService
{
    public interface IUserSubscriptionService
    {
        Task<Response<UserSubscriptionDto>> GetByUserIdAsync(string userId,CancellationToken cancellationToken = default);

        Task<Response<UserSubscriptionDto>> EnsureExistsAsync(string userId,CancellationToken cancellationToken = default);

        Task<Response<UserSubscriptionDto>> SetStarterAsync(string userId,CancellationToken cancellationToken = default);

        Task<Response<UserSubscriptionDto>> SyncFromStripeSubscriptionAsync(Subscription stripeSubscription,CancellationToken cancellationToken = default);

        Task<Response<bool>> HasProfessionalAccessAsync(string userId,CancellationToken cancellationToken = default);

        Task<Response<bool>> CanCreateProjectAsync(string userId,int currentProjectCountThisMonth,CancellationToken cancellationToken = default);
    }
}
