using Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.IPaymentService.StripeService;
using Requra.Infrastructure.Helpers;
using Requra.Infrastructure.Options;
using Stripe;
using Stripe.BillingPortal;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalServices.PaymentService.StripeService
{
    public sealed class StripeBillingService : IStripeBillingService
    {
        private readonly RequraDbContext _dbContext;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly ILogger<StripeBillingService> _logger;

        public StripeBillingService(RequraDbContext dbContext,IOptions<StripeSettings> stripeSettings,ILogger<StripeBillingService> logger)
        {
            _dbContext = dbContext;
            _stripeSettings = stripeSettings;
            _logger = logger;

            StripeConfiguration.ApiKey = _stripeSettings.Value.SecretKey;
        }

        public async Task<Response<string>> CreateCheckoutSessionAsync(string userId, BillingInterval interval, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

                if (user is null)
                    return Response<string>.Failure("User not found.", 404);

                if (interval != BillingInterval.Monthly && interval != BillingInterval.Annual)
                    return Response<string>.Failure("Invalid billing interval.", 400);

                var priceId = GetPriceId(interval);
                if (string.IsNullOrWhiteSpace(priceId))
                    return Response<string>.Failure("Stripe price configuration is missing.", 500);

                var localSubscription = await _dbContext.UserSubscriptions.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

                if (localSubscription is null)
                {
                    localSubscription = new UserSubscription(userId);
                    await _dbContext.UserSubscriptions.AddAsync(localSubscription, cancellationToken);
                }

                if (localSubscription.PlanType == PlanType.Professional &&
                    localSubscription.Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing or SubscriptionStatus.PastDue)
                {
                    return Response<string>.Failure("User already has an active Professional subscription.", 409);
                }

                if (string.IsNullOrWhiteSpace(localSubscription.StripeCustomerId))
                {
                    var customerService = new CustomerService();

                    var customer = await customerService.CreateAsync(new CustomerCreateOptions
                    {
                        Email = user.Email,
                        Name = user.FullName,
                        Metadata = new Dictionary<string, string>
                        {
                            ["userId"] = user.Id,
                            ["email"] = user.Email ?? string.Empty
                        }
                    }, cancellationToken: cancellationToken);

                    localSubscription.SetStripeCustomer(customer.Id);
                }

                var sessionService = new Stripe.Checkout.SessionService();

                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    Mode = "subscription",
                    Customer = localSubscription.StripeCustomerId,
                    SuccessUrl = _stripeSettings.Value.SuccessUrl,    
                    CancelUrl = _stripeSettings.Value.CancelUrl,
                    ClientReferenceId = user.Id,
                    Metadata = new Dictionary<string, string>
                    {
                        ["userId"] = user.Id,
                        ["planType"] = PlanType.Professional.ToString(),
                        ["billingInterval"] = interval.ToString()
                    },
                    LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                    SubscriptionData = new SessionSubscriptionDataOptions
                    {
                        TrialPeriodDays = _stripeSettings.Value.TrialPeriodDays,
                        Metadata = new Dictionary<string, string>
                        {
                            ["userId"] = user.Id,
                            ["planType"] = PlanType.Professional.ToString(),
                            ["billingInterval"] = interval.ToString()
                        }
                    },
                    AllowPromotionCodes = false
                };

                var session = await sessionService.CreateAsync(options, cancellationToken: cancellationToken);

                localSubscription.SetCheckoutSession(session.Id);

                await _dbContext.SaveChangesAsync(cancellationToken);

                return Response<string>.Success(
                    session.Url!,
                    "Checkout session created successfully.",
                    200);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error while creating checkout session for user {UserId}", userId);
                return Response<string>.Failure(
                    $"Stripe error: {ex.StripeError?.Message ?? ex.Message}",
                    502);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating checkout session for user {UserId}", userId);
                return Response<string>.Failure(
                    "An unexpected error occurred while creating checkout session.",
                    500);
            }
        }

        public async Task<Response<string>> CreateCustomerPortalSessionAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var localSubscription = await _dbContext.UserSubscriptions
                    .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

                if (localSubscription is null || string.IsNullOrWhiteSpace(localSubscription.StripeCustomerId))
                {
                    return Response<string>.Failure("Stripe customer not found for this user.", 404);
                }

                var portalService = new Stripe.BillingPortal.SessionService();

                var portalSession = await portalService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
                {
                    Customer = localSubscription.StripeCustomerId,
                    ReturnUrl = _stripeSettings.Value.CustomerPortalReturnUrl
                }, cancellationToken: cancellationToken);

                return Response<string>.Success(
                    portalSession.Url,
                    "Customer portal session created successfully.",
                    200);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error while creating customer portal for user {UserId}", userId);
                return Response<string>.Failure($"Stripe error: {ex.StripeError?.Message ?? ex.Message}", 502);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating customer portal for user {UserId}", userId);
                return Response<string>.Failure("An unexpected error occurred while creating customer portal session.", 500);
            }
        }

        public async Task<Response<bool>> HandleWebhookAsync(string json, string stripeSignature, CancellationToken cancellationToken = default)
        {
            try
            {
                Event stripeEvent;

                try
                {
                    stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _stripeSettings.Value.WebhookSecret);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid Stripe webhook signature.");
                    return Response<bool>.Failure(false, "Invalid Stripe webhook signature.", 400);
                }

                var existingEvent = await _dbContext.StripeWebhookEvents
                    .FirstOrDefaultAsync(x => x.StripeEventId == stripeEvent.Id, cancellationToken);

                if (existingEvent is not null)
                {
                    return Response<bool>.Success(true, "Webhook already processed.", 200);
                }

                var webhookLog = new StripeWebhookEvent(stripeEvent.Id, stripeEvent.Type, json);

                await _dbContext.StripeWebhookEvents.AddAsync(webhookLog, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                switch (stripeEvent.Type)
                {
                    case StripeEventTypes.CheckoutSessionCompleted:
                        await HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken);
                        break;

                    case StripeEventTypes.CustomerSubscriptionCreated:
                    case StripeEventTypes.CustomerSubscriptionUpdated:
                    case StripeEventTypes.CustomerSubscriptionDeleted:
                        await HandleSubscriptionChangedAsync(stripeEvent, cancellationToken);
                        break;

                    case StripeEventTypes.InvoicePaid:
                        case StripeEventTypes.InvoicePaymentSucceeded:
                        await HandleInvoicePaidAsync(stripeEvent, cancellationToken);
                        break;

                    case StripeEventTypes.InvoicePaymentFailed:
                        await HandleInvoicePaymentFailedAsync(stripeEvent, cancellationToken);
                        break;

                    default:
                        _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                        break;
                }
                webhookLog.MarkProcessed();
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Response<bool>.Success(true, "Webhook processed successfully.", 200);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error while processing webhook.");
                return Response<bool>.Failure(false, $"Stripe webhook error: {ex.StripeError?.Message ?? ex.Message}", 502);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing Stripe webhook.");
                return Response<bool>.Failure(false, "An unexpected error occurred while processing webhook.", 500);
            }
        }

        private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent,CancellationToken cancellationToken)
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session is null)
            {
                _logger.LogWarning("checkout.session.completed: session payload is null.");
                return;
            }

            var userId = session.ClientReferenceId;

            if (string.IsNullOrWhiteSpace(userId) &&
                session.Metadata != null &&
                session.Metadata.TryGetValue("userId", out var metadataUserId))
            {
                userId = metadataUserId;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("checkout.session.completed: could not resolve userId.");
                return;
            }

            var localSubscription = await _dbContext.UserSubscriptions
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            if (localSubscription is null)
            {
                localSubscription = new UserSubscription(userId);
                await _dbContext.UserSubscriptions.AddAsync(localSubscription, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(session.CustomerId))
                localSubscription.SetStripeCustomer(session.CustomerId);

            if (!string.IsNullOrWhiteSpace(session.Id))
                localSubscription.SetCheckoutSession(session.Id);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task HandleSubscriptionChangedAsync(Event stripeEvent,CancellationToken cancellationToken)
        {
            var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (stripeSubscription is null)
            {
                _logger.LogWarning("Subscription event payload is null. EventType: {EventType}", stripeEvent.Type);
                return;
            }

            var userId = ResolveUserIdFromSubscription(stripeSubscription);

            if (string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(stripeSubscription.CustomerId))
            {
                var existingLocalSubscription = await _dbContext.UserSubscriptions
                    .FirstOrDefaultAsync(x => x.StripeCustomerId == stripeSubscription.CustomerId, cancellationToken);

                userId = existingLocalSubscription?.UserId;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Could not resolve userId for Stripe subscription {SubscriptionId}", stripeSubscription.Id);
                return;
            }

            var localSubscription = await _dbContext.UserSubscriptions
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            if (localSubscription is null)
            {
                localSubscription = new UserSubscription(userId);
                await _dbContext.UserSubscriptions.AddAsync(localSubscription, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(stripeSubscription.CustomerId))
                localSubscription.SetStripeCustomer(stripeSubscription.CustomerId);

            var firstItem = stripeSubscription.Items?.Data?.FirstOrDefault();
            var price = firstItem?.Price;

            var trialEndsAtUtc = stripeSubscription.TrialEnd;
            var canceledAtUtc = stripeSubscription.CanceledAt;
            var currentPeriodStartUtc = firstItem?.CurrentPeriodStart;
            var currentPeriodEndUtc = firstItem?.CurrentPeriodEnd;

            if (stripeEvent.Type == StripeEventTypes.CustomerSubscriptionDeleted)
            {
                localSubscription.DowngradeToStarter();
            }
            else
            {
                localSubscription.UpdateFromStripe(
                    planType: PlanType.Professional,
                    billingInterval: MapBillingIntervalFromPriceId(price?.Id),
                    status: MapStripeStatus(stripeSubscription.Status),
                    stripeSubscriptionId: stripeSubscription.Id,
                    stripeProductId: price?.ProductId,
                    stripePriceId: price?.Id,
                    trialEndsAtUtc: trialEndsAtUtc,
                    currentPeriodStartUtc: currentPeriodStartUtc,
                    currentPeriodEndUtc: currentPeriodEndUtc,
                    canceledAtUtc: canceledAtUtc,
                    cancelAtPeriodEnd: stripeSubscription.CancelAtPeriodEnd
                );
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        private static string? GetInvoiceSubscriptionId(Invoice invoice)
        {
            if (invoice == null || invoice.Lines == null || invoice.Lines.Data == null || !invoice.Lines.Data.Any())
                return null;

            var subscriptionLine = invoice.Lines.Data.FirstOrDefault(x => x.Subscription != null);

            if (subscriptionLine?.Subscription == null)
                return null;

            return subscriptionLine.Subscription.Id;
        }

        private async Task HandleInvoicePaidAsync(Event stripeEvent,CancellationToken cancellationToken)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice is null)
            {
                _logger.LogWarning("invoice.paid: invoice payload is null.");
                return;
            }
            var stripeSubscriptionId = GetInvoiceSubscriptionId(invoice);
            if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
                return;

            var localSubscription = await _dbContext.UserSubscriptions
                .FirstOrDefaultAsync(x => x.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);

            if (localSubscription is null)
            {
                _logger.LogWarning("invoice.paid: local subscription not found for subscription {SubscriptionId}", stripeSubscriptionId);
                return;
            }

            if (localSubscription.Status == SubscriptionStatus.Trialing)
            {
                localSubscription.UpdateFromStripe(
                    localSubscription.PlanType,
                    localSubscription.BillingInterval,
                    SubscriptionStatus.Active,
                    localSubscription.StripeSubscriptionId,
                    localSubscription.StripeProductId,
                    localSubscription.StripePriceId,
                    localSubscription.TrialEndsAtUtc,
                    localSubscription.CurrentPeriodStartUtc,
                    localSubscription.CurrentPeriodEndUtc,
                    localSubscription.CanceledAtUtc,
                    localSubscription.CancelAtPeriodEnd
                );

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task HandleInvoicePaymentFailedAsync(Event stripeEvent,CancellationToken cancellationToken)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice is null)
            {
                _logger.LogWarning("invoice.payment_failed: invoice payload is null.");
                return;
            }

            var stripeSubscriptionId = GetInvoiceSubscriptionId(invoice);
            if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
                return;

            var localSubscription = await _dbContext.UserSubscriptions
                .FirstOrDefaultAsync(x => x.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);

            if (localSubscription is null)
            {
                _logger.LogWarning("invoice.payment_failed: local subscription not found for subscription {SubscriptionId}", stripeSubscriptionId);
                return;
            }

            localSubscription.UpdateFromStripe(
                localSubscription.PlanType,
                localSubscription.BillingInterval,
                SubscriptionStatus.PastDue,
                localSubscription.StripeSubscriptionId,
                localSubscription.StripeProductId,
                localSubscription.StripePriceId,
                localSubscription.TrialEndsAtUtc,
                localSubscription.CurrentPeriodStartUtc,
                localSubscription.CurrentPeriodEndUtc,
                localSubscription.CanceledAtUtc,
                localSubscription.CancelAtPeriodEnd
            );

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private string GetPriceId(BillingInterval interval)
        {
            return interval switch
            {
                BillingInterval.Monthly => _stripeSettings.Value.ProfessionalMonthlyPriceId,
                BillingInterval.Annual => _stripeSettings.Value.ProfessionalAnnualPriceId,
                _ => string.Empty
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

        private SubscriptionStatus MapStripeStatus(string? stripeStatus)
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

        private string? ResolveUserIdFromSubscription(Stripe.Subscription stripeSubscription)
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
