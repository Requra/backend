using Requra.Application.DTOs.Auth.Login;
using Requra.Application.DTOs.Auth.RefreshToken;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;


namespace Requra.Application.Interfaces.IAuthService
{
    public interface IAuthService
    {
        Task<Response<string>> RegisterAsync(RegisterRequestDto request);
        Task<Response<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenRequest);

        Task<Response<LogInResponseDTO>> LoginAsync(LoginRequestDto request);
        Task<RefreshToken> GetOrCreateRefreshToken(ApplicationUser user);
        Task<RefreshToken> CreateRefreshTokenForLogin(ApplicationUser user, ClientPlatform platform = ClientPlatform.Web);
        Task<Response<string>> LogoutAsync(string userId);


    }


}
