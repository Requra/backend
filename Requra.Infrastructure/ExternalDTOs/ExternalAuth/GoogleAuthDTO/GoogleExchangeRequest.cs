using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalDTOs.ExternalAuth.GoogleAuthDTO
{
    public class GoogleExchangeRequest
    {
        public string IdToken { get; set; } = null!;
        public string? Platform { get; set; } = "web"; // web | android | ios
    }
}
