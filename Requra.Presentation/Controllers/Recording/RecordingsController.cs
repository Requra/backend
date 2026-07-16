using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Meeting;
using Requra.Application.DTOs.Recordings;
using Requra.Application.Interfaces.IRecordingService;
using Requra.Application.Response;
using Requra.Domain.Enums;
using System.Security.Claims;

namespace Requra.Presentation.Controllers.Recording
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class RecordingsController : ControllerBase
    {
        private readonly IRecordingService _recordingService;

        public RecordingsController(IRecordingService recordingService)
        {
            _recordingService = recordingService;
        }

        [HttpGet("recordings/{recordingId:guid}")]
        public async Task<IActionResult> GetRecordingStatus( Guid recordingId, CancellationToken cancellationToken)
        {
            var response = await _recordingService.GetRecordingStatusAsync(recordingId, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                204 => NoContent(),
                400 => BadRequest(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpPost("meetings/{meetingId:guid}/recordings/start")]
        [ProducesResponseType(typeof(Response<StartRecordingResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Response<StartRecordingResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response<StartRecordingResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Response<StartRecordingResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(Response<StartRecordingResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartRecording(Guid meetingId, [FromBody] StartRecordingApiRequest request, CancellationToken cancellationToken)
        {
            var CreatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(CreatedBy))
                return Unauthorized(Response<StartRecordingResponse>.Failure(new StartRecordingResponse(), "Unauthorized User", 401));
            var serviceRequest = new StartRecordingRequest
            {
                MeetingId = meetingId,
                CreatedById = CreatedBy,
                UploadMode = request.UploadMode,
                MimeType = request.MimeType,
            };

            var response = await _recordingService.StartRecordingAsync(serviceRequest, cancellationToken);

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
        public async Task<IActionResult> UploadChunk(
           Guid recordingId,
           [FromForm] UploadChunkRequest request,
           CancellationToken cancellationToken)
        {
            request.RecordingId = recordingId;

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
        public async Task<IActionResult> UploadRecordingFile(Guid recordingId, [FromForm] UploadRecordingFileApiRequest request, CancellationToken cancellationToken)
        {
            var serviceRequest = new UploadRecordingFileRequest
            {
                RecordingId = recordingId,
                File = request.File,
                durationSeconds = request.durationSeconds
            };

            var response = await _recordingService.UploadRecordingFileAsync(serviceRequest, cancellationToken);

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
        public async Task<IActionResult> StopRecording(Guid recordingId, [FromBody] StopRecordingApiRequest request, CancellationToken cancellationToken)
        {

            var serviceRequest = new StopRecordingRequest
            {
               RecordingId= recordingId,
               DurationSeconds= request.DurationSeconds,
               lastChunkIndex = request.lastChunkIndex,
            };

            var response = await _recordingService.StopRecordingAsync(serviceRequest, cancellationToken);

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

       

    }
    }
