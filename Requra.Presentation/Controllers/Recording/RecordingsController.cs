using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Recordings;
using Requra.Application.Interfaces.IRecordingService;
using Requra.Application.Response;
using Requra.Domain.Enums;

namespace Requra.Presentation.Controllers.Recording
{
    [ApiController]
    [Route("api")]
    public class RecordingsController : ControllerBase
    {
        private readonly IRecordingService _recordingService;

        public RecordingsController(IRecordingService recordingService)
        {
            _recordingService = recordingService;
        }

        [HttpPost("meetings/{meetingId:guid}/recordings/start")]
        public async Task<IActionResult> StartRecording(Guid meetingId,[FromBody] StartRecordingRequest request,CancellationToken cancellationToken)
        {
            request.MeetingId = meetingId;

            var response = await _recordingService.StartRecordingAsync(request, cancellationToken);

            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                200 => Ok(response),
                204 => NoContent(),
                400 => BadRequest(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpPost("recordings/{recordingId:guid}/chunks")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> UploadChunk(Guid recordingId,[FromForm] int chunkNumber,[FromForm] string? checksum,[FromForm] IFormFile chunk,CancellationToken cancellationToken)
        {
            if (chunk is null)
            {
                return BadRequest(new
                {
                    message = "Chunk file is required."
                });
            }

            await using var stream = chunk.OpenReadStream();

            var request = new UploadChunkRequest
            {
                RecordingId = recordingId,
                ChunkNumber = chunkNumber,
                ChunkStream = stream,
                FileName = chunk.FileName,
                ContentType = string.IsNullOrWhiteSpace(chunk.ContentType)
                    ? "application/octet-stream"
                    : chunk.ContentType,
                Size = chunk.Length,
                Checksum = checksum
            };

            var response = await _recordingService.UploadChunkAsync(request, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpPost("recordings/{recordingId:guid}/file")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(500_000_000)]
        public async Task<IActionResult> UploadRecordingFile(Guid recordingId,[FromForm] UploadRecordingFileRequest request,CancellationToken cancellationToken)
        {
            request.RecordingId = recordingId;

            var response = await _recordingService.UploadRecordingFileAsync(request, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpPost("recordings/{recordingId:guid}/stop")]
        public async Task<IActionResult> StopRecording(Guid recordingId,[FromBody] StopRecordingRequest request,CancellationToken cancellationToken)
        {
            request.RecordingId = recordingId;

            var response = await _recordingService.StopRecordingAsync(request, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpGet("recordings/{recordingId:guid}")]
        public IActionResult GetRecordingStatus(Guid recordingId)
        {
            return Ok(new
            {
                recordingId,
                message = "Status endpoint placeholder."
            });
        }
    
}
    }
