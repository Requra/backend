using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IAIService;
using Requra.Domain.Enums;

namespace Requra.Infrastructure.ExternalServices.AIClient
{
    //public class FakeAIClient : IAIClient
    //{
    //    public async Task<ProcessJsonResponse> ProcessAsync(ProcessJsonRequest request)
    //    {
    //        await Task.Delay(3000); 

    //        return new ProcessJsonResponse
    //        {
    //            ContractVersion = "1.0.0",
    //            //JobId = request.Job_Id,

    //            Status = AnalysisRunStatus.COMPLETED,

    //            Summary = new SummaryDto
    //            {
    //                ExecutiveSummary = "Short project summary.",
    //                Scope = "Detected project scope.",
    //                MainActors = new List<string> { "Customer", "Admin" },
    //                MainGoals = new List<string> { "Manage orders", "Track inventory" }
    //            },

             

    //            Requirements = new List<RequirementDto>
    //            {
    //                new RequirementDto
    //                {
    //                    Id = "REQ-001",
    //                    Title = "Manage orders",
    //                    Description = "The system should allow users to create and track orders.",
    //                    Type = "Functional",
    //                    Priority = "High",
    //                    ConfidenceScore = 0.91,
    //                    SourceDocumentIds = new List<string> { "doc-uuid-1" }
    //                }
    //            },

    //            UserStories = new List<UserStoryDto>
    //            {
    //                new UserStoryDto
    //                {
    //                    Id = "US-001",
    //                    Title = "Create order",
    //                    Description = "As a customer, I want to create an order so that I can buy products.",
    //                    AcceptanceCriteria = new List<string>
    //                    {
    //                        "Given valid order data, when I submit the order, then the order is created."
    //                    },
    //                    Priority = "High",
    //                    RequirementId = "REQ-001"
    //                }
    //            },

    //            Risks = new List<RiskDto>
    //            {
    //                new RiskDto
    //                {
    //                    Id = "RISK-001",
    //                    Title = "Missing payment provider details",
    //                    Severity = "Medium",
    //                    Description = "Payment integration details were not specified."
    //                }
    //            },

    //            OpenQuestions = new List<OpenQuestionDto>
    //            {
    //                new OpenQuestionDto
    //                {
    //                    Id = "Q-001",
    //                    Question = "Which payment provider should be used?",
    //                    SourceDocumentIds = new List<string> { "doc-uuid-1" }
    //                }
    //            },

    //            ActionItems = new List<ActionItemDto>
    //            {
    //                new ActionItemDto
    //                {
    //                    Id = "ACT-001",
    //                    Title = "Confirm payment provider",
    //                    Owner = null,
    //                    Priority = "High"
    //                }
    //            },
    //            Warnings = new List<string>
    //            {
    //                "Some sections of the document need review."
    //            }

    //        };
    //    }
    //}
}