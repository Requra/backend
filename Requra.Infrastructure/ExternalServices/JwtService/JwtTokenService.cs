using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Requra.Domain.Entities;
using Requra.Infrastructure.ExternalInterfaces.IJwtTokenService;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Requra.Infrastructure.Services.JWTService
{
    internal class JwtTokenService(IConfiguration _config, UserManager<ApplicationUser> _userManager) : IJwtTokenService
    {

        public async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var key = GetSigningKey();
            var issuer = GetRequiredConfig("JWT:Issuer");
            var audience = GetRequiredConfig("JWT:Audience");
            var expiryMinutes = GetPositiveIntConfig("JWT:DurationInMinutes");

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName ?? user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? string.Empty)
        };

            foreach (var role in roles.Distinct())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var now = DateTime.UtcNow;

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(expiryMinutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public RefreshToken CreateRefreshToken()
        {
            var refreshTokenDays = GetPositiveIntConfig("JWT:RefreshTokenDurationInDays");

            var randomNumber = new byte[64];
            RandomNumberGenerator.Fill(randomNumber);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(refreshTokenDays)
            };
        }

        public Task<ClaimsPrincipal> GetPrincipalFromExpiredToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new SecurityTokenException("Access token is required.");
            }

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = GetRequiredConfig("JWT:Issuer"),

                ValidateAudience = true,
                ValidAudience = GetRequiredConfig("JWT:Audience"),

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = GetSigningKey(),

                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero,

                RequireSignedTokens = true,
                RequireExpirationTime = true
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(
                    token,
                    tokenValidationParameters,
                    out SecurityToken securityToken
                );

                if (securityToken is not JwtSecurityToken jwtToken)
                {
                    throw new SecurityTokenException("Invalid access token.");
                }

                if (!string.Equals(
                        jwtToken.Header.Alg,
                        SecurityAlgorithms.HmacSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token algorithm.");
                }

                return Task.FromResult(principal);
            }
            catch (SecurityTokenExpiredException)
            {
                // This is acceptable here because refresh flow validates expired tokens.
                throw;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                throw new SecurityTokenException("Invalid token signature.");
            }
            catch (SecurityTokenInvalidIssuerException)
            {
                throw new SecurityTokenException("Invalid token issuer.");
            }
            catch (SecurityTokenInvalidAudienceException)
            {
                throw new SecurityTokenException("Invalid token audience.");
            }
            catch (SecurityTokenMalformedException)
            {
                throw new SecurityTokenException("Malformed token.");
            }
            catch (SecurityTokenException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new SecurityTokenException("Invalid access token.");
            }
        }

        private SymmetricSecurityKey GetSigningKey()
        {
            var key = GetRequiredConfig("JWT:Key");
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        }

        private string GetRequiredConfig(string key)
        {
            var value = _config[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Missing JWT configuration value: '{key}'.");
            }

            return value;
        }

        private int GetPositiveIntConfig(string key)
        {
            var value = _config.GetValue<int?>(key);

            if (!value.HasValue || value.Value <= 0)
            {
                throw new InvalidOperationException($"Invalid JWT configuration value: '{key}'.");
            }

            return value.Value;
        }


    }
}
