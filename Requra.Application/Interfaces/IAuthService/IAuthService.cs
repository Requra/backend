using Requra.Application.DTOs.Auth.RefreshToken;
using Requra.Application.DTOs.Auth.Register;
using Requra.Application.Response;
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

    }


}
