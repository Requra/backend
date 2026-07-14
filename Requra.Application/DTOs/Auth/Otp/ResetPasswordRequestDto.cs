using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Auth.Otp
{
    public class ResetPasswordRequestDto
    { // we can do it another way by sending the code in the url and then we can get the email from the code but for now we will do it this way
        public string Email { get; set; }
        public string Code { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }
}
