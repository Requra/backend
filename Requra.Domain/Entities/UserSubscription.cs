using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class UserSubscription
    {
        public Guid Id { get; private set; }
        public string UserId { get; private set; } = default!;
        public ApplicationUser User { get; private set; } = default!;

        public PlanType PlanType { get; private set; }
        public BillingInterval BillingInterval { get; private set; }
        public SubscriptionStatus Status { get; private set; }

        public string? StripeCustomerId { get; private set; }
        public string? StripeSubscriptionId { get; private set; }
        public string? StripeProductId { get; private set; }
        public string? StripePriceId { get; private set; }
        public string? StripeCheckoutSessionId { get; private set; }

        public DateTime? TrialEndsAtUtc { get; private set; }
        public DateTime? CurrentPeriodStartUtc { get; private set; }
        public DateTime? CurrentPeriodEndUtc { get; private set; }
        public DateTime? CanceledAtUtc { get; private set; }

        public bool CancelAtPeriodEnd { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }
        public DateTime UpdatedAtUtc { get; private set; }

        private UserSubscription() { }

        public UserSubscription(string userId)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            PlanType = PlanType.Starter;
            BillingInterval = BillingInterval.None;
            Status = SubscriptionStatus.None;
            CreatedAtUtc = DateTime.UtcNow;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void SetStripeCustomer(string customerId)
        {
            StripeCustomerId = customerId;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void SetCheckoutSession(string sessionId)
        {
            StripeCheckoutSessionId = sessionId;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void UpdateFromStripe(
            PlanType planType,
            BillingInterval billingInterval,
            SubscriptionStatus status,
            string? stripeSubscriptionId,
            string? stripeProductId,
            string? stripePriceId,
            DateTime? trialEndsAtUtc,
            DateTime? currentPeriodStartUtc,
            DateTime? currentPeriodEndUtc,
            DateTime? canceledAtUtc,
            bool cancelAtPeriodEnd)
        {
            PlanType = planType;
            BillingInterval = billingInterval;
            Status = status;
            StripeSubscriptionId = stripeSubscriptionId;
            StripeProductId = stripeProductId;
            StripePriceId = stripePriceId;
            TrialEndsAtUtc = trialEndsAtUtc;
            CurrentPeriodStartUtc = currentPeriodStartUtc;
            CurrentPeriodEndUtc = currentPeriodEndUtc;
            CanceledAtUtc = canceledAtUtc;
            CancelAtPeriodEnd = cancelAtPeriodEnd;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void DowngradeToStarter()
        {
            PlanType = PlanType.Starter;
            BillingInterval = BillingInterval.None;
            Status = SubscriptionStatus.None;
            StripeSubscriptionId = null;
            StripeProductId = null;
            StripePriceId = null;
            StripeCheckoutSessionId = null;
            TrialEndsAtUtc = null;
            CurrentPeriodStartUtc = null;
            CurrentPeriodEndUtc = null;
            CanceledAtUtc = null;
            CancelAtPeriodEnd = false;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
