using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Requra.Application.DTOs.Auth.Login;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Requra.Infrastructure.Services.AuthService
{
    public class AuthService() : IAuthService
    {
        
       
    }
    

}

