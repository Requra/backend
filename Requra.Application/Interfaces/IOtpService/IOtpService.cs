using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IOtpService
{
    public interface IOtpService
    {
        Task GenerateAndSendAsync(ApplicationUser user, OtpPurpose purpose);
        Task<Response<bool>> ResendAsync(ApplicationUser user, OtpPurpose purpose);
        Task<Response<bool>> CheckAsync(ApplicationUser user, string code, OtpPurpose purpose);   // read-only peek
        Task<Response<bool>> VerifyAsync(ApplicationUser user, string code, OtpPurpose purpose);
    }
}
