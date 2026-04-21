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

namespace Requra.Infrastructure.Services.JWTService
{
    internal class JwtTokenService(IConfiguration config, UserManager<ApplicationUser> userManager) : IJwtTokenService
    {
        public async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.Role, user.Role.ToString())
        };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["JWT:Key"]!)
            );
            
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryMinutes = int.Parse(config["Jwt:DurationInMinutes"]!);
            var token = new JwtSecurityToken(
                issuer: config["JWT:Issuer"],
                audience: config["JWT:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<string> GenerateRefreshToken()
        {
            var randNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randNumber);
            return Convert.ToBase64String(randNumber);
        }

        public async Task<ClaimsPrincipal> GetPrincipalFromExpiredToken(string token)
        {
            var tokenParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(config["JWT:Key"]!)
                ),
                ValidateLifetime = false 
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(
                    token,
                    tokenParameters,
                    out SecurityToken securityToken
                );

                if (securityToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token algorithm");
                }

                return principal;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                throw new SecurityTokenException("Invalid signature");
            }
            catch (SecurityTokenMalformedException)
            {
                throw new SecurityTokenException("Malformed token");
            }
            catch (Exception)
            {
                throw new SecurityTokenException("Invalid access token");
            }
     
        }


        public async Task<JwtSecurityToken> GenerateJwtToken(ApplicationUser User)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, User.UserName),
                new Claim(ClaimTypes.NameIdentifier, User.Id),
                new Claim(JwtRegisteredClaimNames.Sub, User.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email,User.Email),
                new Claim("uid", User.Id)
            };
            var roles = await userManager.GetRolesAsync(User);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            SecurityKey Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Key"]));
            SigningCredentials signingCred = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);
            var Token = new JwtSecurityToken(
                issuer: config["JWT:issuer"],
                audience: config["JWT:audience"],
                claims: claims,
                signingCredentials: signingCred,
                expires: DateTime.UtcNow.AddDays(1)
                );
            return Token;
        }
        public RefreshToken CreateRefreshToken()
        {
            var randomNumber = new byte[32];
            RandomNumberGenerator.Fill(randomNumber);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                ExpiresOn = DateTime.UtcNow.AddDays(10),
                CreatedOn = DateTime.UtcNow
            };
        }


    }
}
