using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Profile
{
    public class ChangePasswordRequestDto
    { 
        public string CurrentUserId { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
    public class ChangePasswordAPIRequestDto
    {
        public string NewPassword { get; set; }
        public string CurrentPassword { get; set; }
    }
}
