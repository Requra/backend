using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Enums
{
    public enum PlanType
    {
        Starter = 0,
        Professional = 1,
        Enterprise = 2
    }

    public enum BillingInterval
    {
        None = 0,
        Monthly = 1,
        Annual = 2
    }

    public enum SubscriptionStatus
    {
        None = 0,
        Trialing = 1,
        Active = 2,
        PastDue = 3,
        Unpaid = 4,
        Canceled = 5,
        Incomplete = 6,
        IncompleteExpired = 7,
        Paused = 8
    }
}
