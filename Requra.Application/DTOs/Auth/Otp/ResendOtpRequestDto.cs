using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Auth.Otp
{
    public class ResendOtpRequestDto // eaither confirm email or forget password
    {
        public string Email { get; set; }
        public OtpPurpose Purpose { get; set; }
    }
}
