using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.UserSubscription
{
    public sealed class UserSubscriptionDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = default!;
        public PlanType PlanType { get; set; }
        public BillingInterval BillingInterval { get; set; }
        public SubscriptionStatus Status { get; set; }

        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? StripeProductId { get; set; }
        public string? StripePriceId { get; set; }
        public string? StripeCheckoutSessionId { get; set; }

        public DateTime? TrialEndsAtUtc { get; set; }
        public DateTime? CurrentPeriodStartUtc { get; set; }
        public DateTime? CurrentPeriodEndUtc { get; set; }
        public DateTime? CanceledAtUtc { get; set; }

        public bool CancelAtPeriodEnd { get; set; }

        public bool HasProfessionalAccess { get; set; }
    }
}
