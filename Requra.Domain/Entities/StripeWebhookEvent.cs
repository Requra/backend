using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class StripeWebhookEvent
    {
        public Guid Id { get; private set; }
        public string StripeEventId { get; private set; } = default!;
        public string EventType { get; private set; } = default!;
        public bool Processed { get; private set; }
        public DateTime ReceivedAtUtc { get; private set; }
        public DateTime? ProcessedAtUtc { get; private set; }
        public string PayloadJson { get; private set; } = default!;

        private StripeWebhookEvent() { }

        public StripeWebhookEvent(string stripeEventId, string eventType, string payloadJson)
        {
            Id = Guid.NewGuid();
            StripeEventId = stripeEventId;
            EventType = eventType;
            PayloadJson = payloadJson;
            Processed = false;
            ReceivedAtUtc = DateTime.UtcNow;
        }

        public void MarkProcessed()
        {
            Processed = true;
            ProcessedAtUtc = DateTime.UtcNow;
        }
    }
}
