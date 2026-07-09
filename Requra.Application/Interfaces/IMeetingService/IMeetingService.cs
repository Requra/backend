using Requra.Application.DTOs.Meeting;
using Requra.Application.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IMeetingService
{
    public interface IMeetingService
    {
        Task<Response<MeetingDto>> CreateMeetingAsync(
            Guid projectId,
            CreateMeetingRequest request,
            string currentUserId);
    }
}
