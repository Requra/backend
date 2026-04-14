using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Document;
using Requra.Application.Interfaces.IDocumentService;
using Requra.Application.Response;
using System.Net;

namespace Requra.Presentation.Controllers.Document
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController(IDocumentService _documentService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetDocumentsByProjectId([FromQuery] Guid projectId)
        {
            if (projectId == Guid.Empty)
                return BadRequest(Response<string>.Failure("Invalid ProjectId", 400));

            var response = await _documentService.GetDocumentsByProjectIdAsync(projectId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                204 => NoContent(),
                400 => BadRequest(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentDto model,CancellationToken cancellationToken)
        {
            

            if (model.File == null || model.File.Length == 0)
                return BadRequest(Response<string>.Failure("File is required", 400));

            // Get UserId from JWT claims
            //var userId = User.FindFirst("sub")?.Value
            //          ?? User.FindFirst("id")?.Value;

            var userId = "c5008516-f6f3-491f-b336-d500369ba5c9"; //userId for testing

            //if (string.IsNullOrEmpty(userId))
            //    return Unauthorized(Response<string>.Failure("Unauthorized", 401));

            var result = await _documentService.UploadDocumentAsync(model,userId,cancellationToken);

            return result.StatusCode switch
            {
                200 => Ok(result),
                201 => Created(string.Empty, result),
                204 => NoContent(),
                400 => BadRequest(result),
                401 => Unauthorized(result),
                404 => NotFound(result),
                499 => StatusCode(499, result),
                500 => StatusCode(500, result),
                _ => StatusCode(result.StatusCode, result)
            };
        }
    }
}