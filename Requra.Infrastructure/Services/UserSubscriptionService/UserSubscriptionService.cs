using Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Requra.Application.DTOs.UserSubscription;
using Requra.Application.Interfaces.IUserSubscriptionService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Services.UserSubscriptionService
{
    public sealed class UserSubscriptionService(RequraDbContext _dbContext, IOptions<StripeSettings>  _stripeSettings, ILogger<UserSubscriptionService> _logger) : IUserSubscriptionService
    {


        public async Task<Response<UserSubscriptionDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return Response<UserSubscriptionDto>.Failure("UserId is required.", 400);

                var user = await _dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

                if (user is null)
                    return Response<UserSubscriptionDto>.Failure("User not found.", 404);

                var subscription = await _dbContext.UserSubscriptions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

                if (subscription is null)
                {
                    var starterDto = BuildStarterDto(userId);
                    return Response<UserSubscriptionDto>.Success(starterDto, "Starter subscription.", 200);
                }

                return Response<UserSubscriptionDto>.Success(MapToDto(subscription), "Subscription retrieved successfully.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription for user {UserId}", userId);
                return Response<UserSubscriptionDto>.Failure("An unexpected error occurred while retrieving subscription.", 500);
            }
        }

        public async Task<Response<UserSubscriptionDto>> EnsureExistsAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return Response<UserSubscriptionDto>.Failure("UserId is required.", 400);

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

                if (user is null)
                    return Response<UserSubscriptionDto>.Failure("User not found.", 404);

                var subscription = await _dbContext.UserSubscriptions
                    .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

                if (subscription is null)
                {
                    subscription = new UserSubscription(userId);
                    await _dbContext.UserSubscriptions.AddAsync(subscription, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                return Response<UserSubscriptionDto>.Success(MapToDto(subscription), "Subscription ensured successfully.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring subscription for user {UserId}", userId);
                return Response<UserSubscriptionDto>.Failure("An unexpected error occurred while ensuring subscription.", 500);
            }
        }

        public async Task<Response<UserSubscriptionDto>> SetStarterAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return Response<UserSubscriptionDto>.Failure("UserId is required.", 400);

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

                if (user is null)
                    return Response<UserSubscriptionDto>.Failure("User not found.", 404);

                var subscription = await _dbContext.UserSubscriptions
                    .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

                if (subscription is null)
                {
                    subscription = new UserSubscription(userId);
                    await _dbContext.UserSubscriptions.AddAsync(subscription, cancellationToken);
                }
                else
                {
                    subscription.DowngradeToStarter();
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                return Response<UserSubscriptionDto>.Success(MapToDto(subscription), "User downgraded to Starter successfully.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting Starter plan for user {UserId}", userId);
                return Response<UserSubscriptionDto>.Failure("An unexpected error occurred while setting Starter plan.", 500);
            }
        }

        public async Task<Response<UserSubscriptionDto>> SyncFromStripeSubscriptionAsync(Stripe.Subscription stripeSubscription, CancellationToken cancellationToken = default)
        {
            try
            {
                if (stripeSubscription is null)
                    return Response<UserSubscriptionDto>.Failure("Stripe subscription is required.", 400);

                var userId = ResolveUserIdFromStripeSubscription(stripeSubscription);

                if (string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(stripeSubscription.CustomerId))
                {
                    var existingByCustomer = await _dbContext.UserSubscriptions
                        .FirstOrDefaultAsync(x => x.StripeCustomerId == stripeSubscription.CustomerId, cancellationToken);

                    userId = existingByCustomer?.UserId;
                }

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Unable to resolve local user id from Stripe subscription {SubscriptionId}", stripeSubscription.Id);

                    return Response<UserSubscriptionDto>.Failure("Could not resolve user for Stripe subscription.", 404);
                }

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

                if (user is null)
                    return Response<UserSubscriptionDto>.Failure("User not found.", 404);

                var subscription = await _dbContext.UserSubscriptions
                    .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

                if (subscription is null)
                {
                    subscription = new UserSubscription(userId);
                    await _dbContext.UserSubscriptions.AddAsync(subscription, cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(stripeSubscription.CustomerId))
                    subscription.SetStripeCustomer(stripeSubscription.CustomerId);

                var firstItem = stripeSubscription.Items?.Data?.FirstOrDefault();
                var price = firstItem?.Price;

                var priceId = price?.Id;
                var productId = price?.ProductId;

                var billingInterval = MapBillingIntervalFromPriceId(priceId);
                var status = MapStripeStatus(stripeSubscription.Status);

                var trialEndsAtUtc = stripeSubscription.TrialEnd;
                var canceledAtUtc = stripeSubscription.CanceledAt;
                var currentPeriodStartUtc = firstItem?.CurrentPeriodStart;
                var currentPeriodEndUtc = firstItem?.CurrentPeriodEnd;

                if (status == SubscriptionStatus.Canceled)
                {
                    subscription.DowngradeToStarter();
                }
                else
                {
                    subscription.UpdateFromStripe(
                        planType: PlanType.Professional,
                        billingInterval: billingInterval,
                        status: status,
                        stripeSubscriptionId: stripeSubscription.Id,
                        stripeProductId: productId,
                        stripePriceId: priceId,
                        trialEndsAtUtc: trialEndsAtUtc,
                        currentPeriodStartUtc: currentPeriodStartUtc,
                        currentPeriodEndUtc: currentPeriodEndUtc,
                        canceledAtUtc: canceledAtUtc,
                        cancelAtPeriodEnd: stripeSubscription.CancelAtPeriodEnd
                    );
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                return Response<UserSubscriptionDto>.Success(MapToDto(subscription), "Subscription synced successfully from Stripe.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing local subscription from Stripe subscription {SubscriptionId}", stripeSubscription?.Id);
                return Response<UserSubscriptionDto>.Failure("An unexpected error occurred while syncing subscription from Stripe.", 500);
            }
        }

        public async Task<Response<bool>> HasProfessionalAccessAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return Response<bool>.Failure("UserId is required.", 400);

                var userExists = await _dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == userId, cancellationToken);

                if (!userExists)
                    return Response<bool>.Failure("User not found.", 404);

                var subscription = await _dbContext.UserSubscriptions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

                if (subscription is null)
                    return Response<bool>.Success(false, "User is on Starter plan.", 200);

                var hasAccess = HasProfessionalAccess(subscription);

                return Response<bool>.Success(
                    hasAccess,
                    hasAccess ? "User has Professional access." : "User does not have Professional access.",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Professional access for user {UserId}", userId);
                return Response<bool>.Failure(
                    "An unexpected error occurred while checking Professional access.",
                    500);
            }
        }

        public async Task<Response<bool>> CanCreateProjectAsync(
            string userId,
            int currentProjectCountThisMonth,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return Response<bool>.Failure("UserId is required.", 400);

                if (currentProjectCountThisMonth < 0)
                    return Response<bool>.Failure("Project count cannot be negative.", 400);

                var userExists = await _dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == userId, cancellationToken);

                if (!userExists)
                    return Response<bool>.Failure("User not found.", 404);

                var subscription = await _dbContext.UserSubscriptions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

                if (subscription is null)
                {
                    var starterCanCreate = currentProjectCountThisMonth < 5;
                    return Response<bool>.Success(
                        starterCanCreate,
                        starterCanCreate
                            ? "Starter user can create project."
                            : "Starter plan monthly project limit reached.",
                        200);
                }

                if (HasProfessionalAccess(subscription))
                {
                    return Response<bool>.Success(true, "Professional user can create project.", 200);
                }

                var canCreateUnderStarter = currentProjectCountThisMonth < 5;

                return Response<bool>.Success(
                    canCreateUnderStarter,
                    canCreateUnderStarter
                        ? "Starter user can create project."
                        : "Starter plan monthly project limit reached.",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking project creation eligibility for user {UserId}", userId);
                return Response<bool>.Failure(
                    "An unexpected error occurred while checking project creation eligibility.",
                    500);
            }
        }

        private static bool HasProfessionalAccess(UserSubscription subscription)
        {
            return subscription.PlanType == PlanType.Professional &&
                   (subscription.Status == SubscriptionStatus.Active ||
                    subscription.Status == SubscriptionStatus.Trialing);
        }

        private UserSubscriptionDto MapToDto(UserSubscription subscription)
        {
            return new UserSubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                PlanType = subscription.PlanType,
                BillingInterval = subscription.BillingInterval,
                Status = subscription.Status,
                StripeCustomerId = subscription.StripeCustomerId,
                StripeSubscriptionId = subscription.StripeSubscriptionId,
                StripeProductId = subscription.StripeProductId,
                StripePriceId = subscription.StripePriceId,
                StripeCheckoutSessionId = subscription.StripeCheckoutSessionId,
                TrialEndsAtUtc = subscription.TrialEndsAtUtc,
                CurrentPeriodStartUtc = subscription.CurrentPeriodStartUtc,
                CurrentPeriodEndUtc = subscription.CurrentPeriodEndUtc,
                CanceledAtUtc = subscription.CanceledAtUtc,
                CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                HasProfessionalAccess = HasProfessionalAccess(subscription)
            };
        }

        private static UserSubscriptionDto BuildStarterDto(string userId)
        {
            return new UserSubscriptionDto
            {
                Id = Guid.Empty,
                UserId = userId,
                PlanType = PlanType.Starter,
                BillingInterval = BillingInterval.None,
                Status = SubscriptionStatus.None,
                StripeCustomerId = null,
                StripeSubscriptionId = null,
                StripeProductId = null,
                StripePriceId = null,
                StripeCheckoutSessionId = null,
                TrialEndsAtUtc = null,
                CurrentPeriodStartUtc = null,
                CurrentPeriodEndUtc = null,
                CanceledAtUtc = null,
                CancelAtPeriodEnd = false,
                HasProfessionalAccess = false
            };
        }

        private BillingInterval MapBillingIntervalFromPriceId(string? priceId)
        {
            if (string.IsNullOrWhiteSpace(priceId))
                return BillingInterval.None;

            if (priceId == _stripeSettings.Value.ProfessionalMonthlyPriceId)
                return BillingInterval.Monthly;

            if (priceId == _stripeSettings.Value.ProfessionalAnnualPriceId)
                return BillingInterval.Annual;

            return BillingInterval.None;
        }

        private static SubscriptionStatus MapStripeStatus(string? stripeStatus)
        {
            return stripeStatus switch
            {
                "trialing" => SubscriptionStatus.Trialing,
                "active" => SubscriptionStatus.Active,
                "past_due" => SubscriptionStatus.PastDue,
                "unpaid" => SubscriptionStatus.Unpaid,
                "canceled" => SubscriptionStatus.Canceled,
                "incomplete" => SubscriptionStatus.Incomplete,
                "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
                "paused" => SubscriptionStatus.Paused,
                _ => SubscriptionStatus.None
            };
        }

        private static string? ResolveUserIdFromStripeSubscription(Stripe.Subscription stripeSubscription)
        {
            if (stripeSubscription.Metadata != null &&
                stripeSubscription.Metadata.TryGetValue("userId", out var userId) &&
                !string.IsNullOrWhiteSpace(userId))
            {
                return userId;
            }

            return null;
        }
    }
}
