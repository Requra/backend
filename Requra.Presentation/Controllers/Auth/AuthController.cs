using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Auth.Login;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Response;
using Requra.Infrastructure.Services.AuthService;
using static System.Net.WebRequestMethods;

namespace Requra.Presentation.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        
    }
}

