using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalServices.AIClient
{ // when the AI service returns an error, we throw this exception with the upstream status code and message
    public class AIServiceException : Exception
    {
        public int UpstreamStatusCode { get; }

        public AIServiceException(string message, int upstreamStatusCode, Exception? inner = null)
            : base(message, inner)
        {
            UpstreamStatusCode = upstreamStatusCode;
        }
    }
}
