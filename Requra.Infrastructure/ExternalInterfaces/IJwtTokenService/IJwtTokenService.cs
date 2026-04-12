using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Requra.Infrastructure.ExternalInterfaces.IJwtTokenService
{
    public interface IJwtTokenService
    {
        Task<string> GenerateTokenAsync(ApplicationUser user);
      public Task<string> GenerateRefreshToken();
      public Task<ClaimsPrincipal?> GetPrincipalFromExpiredToken(string token);


    }
}
