using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Helpers
{
    public static class StripeEventTypes
    {
        public const string CheckoutSessionCompleted = "checkout.session.completed";

        public const string CustomerSubscriptionCreated = "customer.subscription.created";
        public const string CustomerSubscriptionUpdated = "customer.subscription.updated";
        public const string CustomerSubscriptionDeleted = "customer.subscription.deleted";

        public const string InvoicePaid = "invoice.paid";
        public const string InvoicePaymentFailed = "invoice.payment_failed";
        public const string InvoicePaymentSucceeded = "invoice.payment_succeeded";
    }
}
