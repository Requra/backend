using Requra.Application.DTOs.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IAIService
{
    public interface IAIClient
    {
        Task<ProcessJsonResponse> ProcessAsync(ProcessJsonRequest request);
    }
}
