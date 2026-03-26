using Microsoft.AspNetCore.Identity;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string JobTitle { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogIn { get; set; }
        public UserRole Role { get; set; }


        // Foreign Key

        #region Navigation Property




        #endregion


    }

}
