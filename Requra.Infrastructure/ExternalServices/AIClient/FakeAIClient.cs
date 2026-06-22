using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IAIService;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalServices.AIClient
{
    public class FakeAIClient : IAIClient
    {
        public async Task<ProcessJsonResponse> ProcessAsync(ProcessJsonRequest request)
        {
            await Task.Delay(2000);

            return new ProcessJsonResponse
            {
                ContractVersion = "1.0",
                JobId = request.Job_Id,
                Status = AnalysisRunStatus.COMPLETED,

                Summary = new SummaryDto
                {
                    Overview = "This is a simulated AI-generated summary.",
      
                    KeyPoints = new List<string> { "Manage system", "Improve workflow" }
                },

                Requirements = new List<RequirementDto>
            {
                new RequirementDto
                {
                    Id = "REQ-001",
                    Description = "AI-generated fake requirement for testing.",
                
                }
            },

                Risks = new List<RiskDto>
            {
                new RiskDto
                {
                  
                    Severity = "Medium",
                    Description = "This is a simulated risk."
                }
            },

                OpenQuestions = new List<OpenQuestionDto>
            {
                new OpenQuestionDto
                {
                    //id = "Q-001",
                    Question = "What is the real business logic?",
                }
            },

                ActionItems = new List<ActionItemDto>
            {
                new ActionItemDto
                {
                    Task = "Review requirements"
                
                }
            }
            };
        }
    }
}
