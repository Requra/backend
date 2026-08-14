using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IAIService;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Requra.Infrastructure.Services.ExportService
{
    public class ExcelExportService : IExcelExportService
    {
        private readonly ILogger<ExcelExportService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICloudinaryService _cloudinaryService;

        public ExcelExportService(
            ILogger<ExcelExportService> logger, 
            IConfiguration configuration,
            ICloudinaryService cloudinaryService)
        {
            _logger = logger;
            _configuration = configuration;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ExportResultsDto> GenerateExcelExportAsync(
            List<RequirementDto> requirements,
            List<UserStoryDto> userStories,
            Guid projectId,
            string format = "xlsx")
        {
            try
            {
                // Generate Excel file
                var fileName = $"project-results-dashboard-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
                var fileStream = GenerateExcelStream(requirements, userStories);

                // Upload to Cloudinary using stream
                var cloudinaryFileName = $"exports/xlsx/{projectId}/{fileName}";
                var uploadedFile = await _cloudinaryService.UploadStreamAsync(
                    fileStream, 
                    fileName, 
                    cloudinaryFileName,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

                return new ExportResultsDto
                {
                    FileName = fileName,
                    FileUrl = uploadedFile.Url,
                    Format = "xlsx",
                    ExpiresAt = DateTime.UtcNow.AddDays(7) // Expires in 7 days
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel export for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<ExportResultsDto> GenerateCsvExportAsync(
            List<RequirementDto> requirements,
            List<UserStoryDto> userStories,
            Guid projectId)
        {
            try
            {
                // Generate CSV file
                var fileName = $"project-results-dashboard-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
                var csvContent = GenerateCsvContent(requirements, userStories);

                // Convert string content to stream
                var csvBytes = Encoding.UTF8.GetBytes(csvContent);
                var csvStream = new MemoryStream(csvBytes);

                // Upload to Cloudinary using stream
                var cloudinaryFileName = $"exports/csv/{projectId}/{fileName}";
                var uploadedFile = await _cloudinaryService.UploadStreamAsync(
                    csvStream,
                    fileName,
                    cloudinaryFileName,
                    "text/csv");

                return new ExportResultsDto
                {
                    FileName = fileName,
                    FileUrl = uploadedFile.Url,
                    Format = "csv",
                    ExpiresAt = DateTime.UtcNow.AddDays(7) // Expires in 7 days
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CSV export for project {ProjectId}", projectId);
                throw;
            }
        }

        private MemoryStream GenerateExcelStream(List<RequirementDto> requirements, List<UserStoryDto> userStories)
        {
            var memoryStream = new MemoryStream();
            using (var spreadsheetDocument = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = spreadsheetDocument.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet();

                var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                var sheet = new Sheet()
                {
                    Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Results"
                };
                sheets.Append(sheet);

                var sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());

                // Add headers
                var headerRow = new Row { RowIndex = 1 };
                AddCell(headerRow, "Type", 1, CellValues.String);
                AddCell(headerRow, "Id", 2, CellValues.String);
                AddCell(headerRow, "Title", 3, CellValues.String);
                AddCell(headerRow, "Description", 4, CellValues.String);
                AddCell(headerRow, "Status", 5, CellValues.String);
                sheetData.Append(headerRow);

                uint rowIndex = 2;

                // Add requirements
                foreach (var req in requirements)
                {
                    var row = new Row { RowIndex = rowIndex };
                    AddCell(row, "Requirement", 1, CellValues.String);
                    AddCell(row, req.Id, 2, CellValues.String);
                    AddCell(row, req.Title, 3, CellValues.String);
                    AddCell(row, req.Description ?? "", 4, CellValues.String);
                    AddCell(row, "Approved", 5, CellValues.String);
                    sheetData.Append(row);
                    rowIndex++;
                }

                // Add user stories
                foreach (var story in userStories)
                {
                    var row = new Row { RowIndex = rowIndex };
                    AddCell(row, "User Story", 1, CellValues.String);
                    AddCell(row, story.Id, 2, CellValues.String);
                    AddCell(row, story.Title, 3, CellValues.String);
                    AddCell(row, story.UserStory ?? "", 4, CellValues.String);
                    AddCell(row, "Approved", 5, CellValues.String);
                    sheetData.Append(row);
                    rowIndex++;
                }

                workbookPart.Workbook.Save();
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        private string GenerateCsvContent(List<RequirementDto> requirements, List<UserStoryDto> userStories)
        {
            var sb = new StringBuilder();

            // Add headers
            sb.AppendLine("Type,Id,Title,Description,Status");

            // Add requirements
            foreach (var req in requirements)
            {
                var description = req.Description ?? "";
                var escapedDescription = EscapeCsvField(description);
                sb.AppendLine($"Requirement,{req.Id},{EscapeCsvField(req.Title)},{escapedDescription},Approved");
            }

            // Add user stories
            foreach (var story in userStories)
            {
                var description = story.UserStory ?? "";
                var escapedDescription = EscapeCsvField(description);
                sb.AppendLine($"User Story,{story.Id},{EscapeCsvField(story.Title)},{escapedDescription},Approved");
            }

            return sb.ToString();
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            if (field.Contains("\"") || field.Contains(",") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        private void AddCell(Row row, string value, int columnIndex, CellValues dataType)
        {
            var cell = new Cell
            {
                CellReference = GetColumnLetters(columnIndex) + row.RowIndex,
                DataType = dataType,
                CellValue = new CellValue(value)
            };
            row.Append(cell);
        }

        private string GetColumnLetters(int columnNumber)
        {
            var columnLetters = "";
            while (columnNumber > 0)
            {
                columnNumber--;
                columnLetters = Convert.ToChar(65 + (columnNumber % 26)) + columnLetters;
                columnNumber /= 26;
            }
            return columnLetters;
        }
    }
}
