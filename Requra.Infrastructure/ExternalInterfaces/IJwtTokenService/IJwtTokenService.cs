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

        Task<ClaimsPrincipal?> GetPrincipalFromExpiredToken(string token);
        RefreshToken CreateRefreshToken();
        Task<string> GenerateAccessTokenAsync(ApplicationUser user);


    }
}
