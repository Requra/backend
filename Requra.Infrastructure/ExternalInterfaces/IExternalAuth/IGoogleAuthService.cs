using Requra.Application.DTOs.Auth.Login;
using Requra.Application.Response;
using Requra.Infrastructure.ExternalDTOs.ExternalAuth.GoogleAuthDTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalInterfaces.IExternalAuth
{
    public interface IGoogleAuthService
    {
        Task<Response<LogInResponseDTO>> GoogleLogin(string googleToken, string platform = "web");
    }
}
