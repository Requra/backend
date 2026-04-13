using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.Interfaces.IDocumentService;
using Requra.Application.Response;

namespace Requra.Presentation.Controllers.Document
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController(IDocumentService _documentService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetDocumentsByProjectId(Guid projectId)
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
    }
}
