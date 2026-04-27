using Requra.Application.DTOs.Auth.RefreshToken;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.Response;
using Requra.Domain.Entities;
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


        Task<RefreshToken> GetOrCreateRefreshToken(ApplicationUser user);
        Task<Response<string>> LogoutAsync(string userId);


    }


}
