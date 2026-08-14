using Requra.Application.DTOs.AI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Requra.Application.Interfaces.IAIService
{
    public interface IExcelExportService
    {
        /// <summary>
        /// Generates an Excel export containing only approved requirements and user stories
        /// </summary>
        Task<ExportResultsDto> GenerateExcelExportAsync(List<RequirementDto> requirements, List<UserStoryDto> userStories, Guid projectId, string format = "xlsx");

        /// <summary>
        /// Generates a CSV export containing only approved requirements and user stories
        /// </summary>
        Task<ExportResultsDto> GenerateCsvExportAsync(List<RequirementDto> requirements, List<UserStoryDto> userStories, Guid projectId);
    }
}
