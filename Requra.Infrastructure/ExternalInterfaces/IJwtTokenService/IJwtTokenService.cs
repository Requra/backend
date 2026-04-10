using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalInterfaces.IJwtTokenService
{
    public interface IJwtTokenService
    {
      Task<string> GenerateTokenAsync(ApplicationUser user);
    }
}
