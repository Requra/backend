using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Options
{
    public class StripeSettings
    {
        public const string SectionName = "Stripe";

        public string SecretKey { get; set; } = default!;
        public string PublishableKey { get; set; } = default!;
        public string WebhookSecret { get; set; } = default!;
        public string ProfessionalMonthlyPriceId { get; set; } = default!;
        public string ProfessionalAnnualPriceId { get; set; } = default!;
        public string SuccessUrl { get; set; } = default!;
        public string CancelUrl { get; set; } = default!;
        public string CustomerPortalReturnUrl { get; set; } = default!;
        public string ProfessionalProductId { get; set; } = default!;
        public int TrialPeriodDays { get; set; } = 14;
    }
}
