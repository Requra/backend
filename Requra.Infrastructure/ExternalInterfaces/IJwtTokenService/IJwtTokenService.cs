using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Requra.Infrastructure.ExternalInterfaces.IJwtTokenService
{
    public interface IJwtTokenService
    {
      Task<string> GenerateTokenAsync(ApplicationUser user);
      public Task<string> GenerateRefreshToken();
      public Task<ClaimsPrincipal?> GetPrincipalFromExpiredToken(string token);


        Task<JwtSecurityToken> GenerateJwtToken(ApplicationUser User);
        RefreshToken CreateRefreshToken();


    }
}
