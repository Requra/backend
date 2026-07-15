using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Auth.Otp
{
    public class ConfirmAccountRequestDto
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
