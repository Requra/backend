using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class IdempotencyRecord
    {
        public Guid Id { get; private set; }

        public string Key { get; private set; } = null!;

        /// <summary>Logical operation this key was scoped to, e.g. "user-story-regenerate".</summary>
        public string Scope { get; private set; } = null!;

        /// <summary>SHA-256 hex hash of the normalized request payload.</summary>
        public string RequestHash { get; private set; } = null!;

        /// <summary>Serialized JSON of the full response envelope that was returned.</summary>
        public string ResponseBody { get; private set; } = null!;

        public int StatusCode { get; private set; }

        public DateTime CreatedAt { get; private set; }

        private IdempotencyRecord()
        {
        }

        public IdempotencyRecord(string key, string scope, string requestHash, string responseBody, int statusCode)
        {
            Id = Guid.NewGuid();
            Key = key;
            Scope = scope;
            RequestHash = requestHash;
            ResponseBody = responseBody;
            StatusCode = statusCode;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
