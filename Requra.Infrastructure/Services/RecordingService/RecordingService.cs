using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Requra.Application.DTOs.Document;
using Requra.Application.DTOs.Recordings;
using Requra.Application.Interfaces.IRecordingService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Services.RecordingService
{
    public class RecordingService(RequraDbContext _context, ICloudinaryService _cloudinaryService, IRecordingBackgroundJobService _backgroundJobService) : IRecordingService
    {
        private const string ActiveRecordingConstraintName = "ux_recordings_meeting_id_one_active";
        private const string ChunkUniqueConstraintName = "ux_recording_chunks_recording_id_chunk_number";

        public async Task<Response<StartRecordingResponse>> StartRecordingAsync(StartRecordingRequest request,CancellationToken cancellationToken = default)
        {
            var validationErrors = ValidateStartRequest(request);
            if (validationErrors.Any())
                return Response<StartRecordingResponse>.Failure(new StartRecordingResponse(),"Validation failed.",400,validationErrors);

            try
            {
                var meetingExists = await _context.MeetingSessions
                    .AsNoTracking()
                    .AnyAsync(m => m.Id == request.MeetingId, cancellationToken);

                if (!meetingExists)
                {
                    return Response<StartRecordingResponse>.Failure(new StartRecordingResponse(),"Meeting not found.",404);
                }

                var hasActiveRecording = await _context.Recordings
                    .AsNoTracking()
                    .AnyAsync(r =>
                        r.MeetingId == request.MeetingId &&
                        (r.Status == RecordingStatus.Started ||
                         r.Status == RecordingStatus.Uploading ||
                         r.Status == RecordingStatus.Ending),
                        cancellationToken);

                if (hasActiveRecording)
                {
                    return Response<StartRecordingResponse>.Failure(new StartRecordingResponse(), "An active recording already exists for this meeting.", 409);
                }

                var recording = new Recording(
                    request.MeetingId,
                    request.CreatedById,
                    request.FileName,
                    request.UploadMode,
                    request.ContentType,
                    request.OriginalExtension,
                    request.ExpectedChunks);

                await _context.Recordings.AddAsync(recording, cancellationToken);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (IsActiveRecordingUniqueViolation(ex))
                {
                    return Response<StartRecordingResponse>.Failure(new StartRecordingResponse(), "There is already an active recording for this meeting.", 409);
                }

                var response = new StartRecordingResponse
                {
                    RecordingId = recording.Id,
                    MeetingId = recording.MeetingId,
                    FileName = recording.FileName,
                    UploadMode = recording.UploadMode,
                    Status = recording.Status,
                    StartedAt = recording.StartedAt
                };

                return Response<StartRecordingResponse>.Success(response,"Recording session started successfully.",201);
            }
            catch (Exception ex)
            {
                return Response<StartRecordingResponse>.Failure(new StartRecordingResponse(),"An unexpected error occurred while starting the recording.",500,new List<string> { ex.Message });
            }
        }

        public async Task<Response<UploadChunkResponse>> UploadChunkAsync(UploadChunkRequest request,CancellationToken cancellationToken = default)
        {
            var validationErrors = ValidateChunkRequest(request);
            if (validationErrors.Any())
                return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Validation failed.",400,validationErrors);

            try
            {
                var recording = await _context.Recordings
                    .FirstOrDefaultAsync(r => r.Id == request.RecordingId, cancellationToken);

                if (recording is null)
                {
                    return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Recording not found.",404);
                }

                var stateErrors = ValidateRecordingCanAcceptChunk(recording, request.ContentType);
                if (stateErrors.Any())
                {
                    return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Recording cannot accept this chunk.",409,stateErrors);
                }

                if (recording.UploadMode != RecordingUploadMode.Chunked)
                {
                    return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"This recording does not accept chunk uploads.",400);
                }

                var existingChunk = await _context.RecordingChunks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.RecordingId == request.RecordingId && c.ChunkNumber == request.ChunkNumber,cancellationToken);

                if (existingChunk is not null)
                {
                    var duplicateResponse = BuildDuplicateChunkResponse(recording, existingChunk, request);
                    if (!duplicateResponse.IsSuccess)
                        return duplicateResponse;

                    return duplicateResponse;
                }

                if (request.ChunkStream.CanSeek)
                    request.ChunkStream.Position = 0;

                var uploadResult = await _cloudinaryService.UploadStreamAsync(
                    request.ChunkStream,
                    fileName: request.FileName,
                    folderName: $"recordings/{recording.MeetingId}/{recording.Id}/chunks",
                    contentType: request.ContentType,
                    cancellationToken: cancellationToken,
                    publicId: $"chunk-{request.ChunkNumber}",
                    overwrite: false);

                if (!uploadResult.IsSuccess || string.IsNullOrWhiteSpace(uploadResult.PublicId) || string.IsNullOrWhiteSpace(uploadResult.Url))
                {
                    return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Failed to upload chunk to cloud storage.",500,new List<string> { uploadResult.ErrorMessage ?? "Unknown cloud upload error." });
                }

                var chunk = new RecordingChunk(
                    request.RecordingId,
                    request.ChunkNumber,
                    uploadResult.Url!,
                    request.Size,
                    uploadResult.PublicId,
                    uploadResult.PublicId,
                    request.Checksum,
                    request.ContentType);

                await _context.RecordingChunks.AddAsync(chunk, cancellationToken);

                recording.MarkUploading();
                recording.RegisterChunk(request.Size);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await SafeDeleteChunkFromCloudAsync(uploadResult.PublicId, cancellationToken);

                    return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"A concurrency conflict occurred while saving the uploaded chunk.",409,new List<string> { ex.Message });
                }
                catch (DbUpdateException ex) when (IsChunkUniqueViolation(ex))
                {
                    await SafeDeleteChunkFromCloudAsync(uploadResult.PublicId, cancellationToken);

                    var duplicateChunk = await _context.RecordingChunks
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.RecordingId == request.RecordingId && c.ChunkNumber == request.ChunkNumber,cancellationToken);

                    if (duplicateChunk is null)
                    {
                        return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Chunk upload conflict occurred.",409);
                    }

                    return BuildDuplicateChunkResponse(recording, duplicateChunk, request);
                }
                catch (DbUpdateException ex)
                {
                    await SafeDeleteChunkFromCloudAsync(uploadResult.PublicId, cancellationToken);

                    return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"A database error occurred while saving the uploaded chunk.",500,new List<string> { ex.Message });
                }

                var response = new UploadChunkResponse
                {
                    RecordingId = recording.Id,
                    ChunkId = chunk.Id,
                    ChunkNumber = chunk.ChunkNumber,
                    ChunkStatus = chunk.Status,
                    RecordingStatus = recording.Status,
                    IsDuplicate = false,
                    UploadedChunks = recording.UploadedChunks,
                    ExpectedChunks = recording.ExpectedChunks,
                    ReceivedBytes = recording.ReceivedBytes,
                    UploadedAt = chunk.UploadedAt
                };

                return Response<UploadChunkResponse>.Success(response,"Chunk uploaded successfully.",200);
            }
            catch (Exception ex)
            {
                return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"An unexpected error occurred while uploading the chunk.",500,new List<string> { ex.Message });
            }
        }

        public async Task<Response<UploadRecordingFileResponse>> UploadRecordingFileAsync(UploadRecordingFileRequest request,CancellationToken cancellationToken = default)
        {
            var validationErrors = ValidateFileUploadRequest(request);
            if (validationErrors.Any())
                return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Validation failed.",400,validationErrors);

            try
            {
                var recording = await _context.Recordings.FirstOrDefaultAsync(r => r.Id == request.RecordingId, cancellationToken);

                if (recording is null)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Recording not found.",404);
                }

                if (recording.UploadMode != RecordingUploadMode.SingleFile)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"This recording does not accept complete file upload.",400);
                }

                if (recording.Status == RecordingStatus.Completed)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Recording is already completed.",409);
                }

                if (recording.Status == RecordingStatus.Ending)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Recording is currently ending.",409);
                }

                if (recording.Status == RecordingStatus.Failed)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Recording is marked as failed.",409);
                }

                if (recording.Status == RecordingStatus.Expired)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Recording is expired.",409);
                }

                if (!string.IsNullOrWhiteSpace(recording.ContentType) &&!string.Equals(recording.ContentType, request.File.ContentType, StringComparison.OrdinalIgnoreCase))
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Uploaded file content type does not match recording content type.",400);
                }

                var uploadResult = await _cloudinaryService.UploadFileAsync(
                    request.File,
                    folderName: $"recordings/{recording.MeetingId}/{recording.Id}",
                    cancellationToken: cancellationToken,
                    publicId: "recording-final",
                    overwrite: true);

                if (!uploadResult.IsSuccess || string.IsNullOrWhiteSpace(uploadResult.PublicId) || string.IsNullOrWhiteSpace(uploadResult.Url))
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Failed to upload file to cloud storage.",500,new List<string> { uploadResult.ErrorMessage ?? "Unknown cloud upload error." });
                }

                recording.MarkUploading();
                recording.SetStorage(uploadResult.Url,uploadResult.PublicId,uploadResult.PublicId,request.File.Length);
                recording.MarkCompleted();

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"A concurrency error occurred while saving the recording file.",409,new List<string> { ex.Message });
                }
                catch (DbUpdateException ex)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"A database error occurred while saving the recording file.",500,new List<string> { ex.Message });
                }

                var response = new UploadRecordingFileResponse
                {
                    RecordingId = recording.Id,
                    StorageUrl = recording.StorageUrl!,
                    PublicId = recording.PublicId!,
                    FinalFileSizeBytes = recording.FinalFileSizeBytes ?? request.File.Length,
                    Status = recording.Status,
                    CompletedAt = recording.CompletedAt
                };

                return Response<UploadRecordingFileResponse>.Success(response,"Recording file uploaded successfully.",200);
            }
            catch (Exception ex)
            {
                return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"An unexpected error occurred while uploading the recording file.",500,new List<string> { ex.Message });
            }
        }

        public async Task<Response<StopRecordingResponse>> StopRecordingAsync(StopRecordingRequest request,CancellationToken cancellationToken = default)
        {
            var validationErrors = ValidateStopRequest(request);
            if (validationErrors.Any())
                return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"Validation failed.",400,validationErrors);

            try
            {
                var recording = await _context.Recordings.FirstOrDefaultAsync(r => r.Id == request.RecordingId, cancellationToken);

                if (recording is null)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"Recording not found.",404);
                }

                if (recording.UploadMode != RecordingUploadMode.Chunked)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"Stop endpoint is only valid for chunked recordings.",400);
                }

                if (recording.Status == RecordingStatus.Completed)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"Recording is already completed.",409);
                }

                if (recording.Status == RecordingStatus.Failed)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"Recording is marked as failed.",409);
                }

                if (recording.Status == RecordingStatus.Expired)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"Recording is expired.",409);
                }

                if (recording.Status == RecordingStatus.Ending)
                {
                    var alreadyFinalizingResponse = new StopRecordingResponse
                    {
                        RecordingId = recording.Id,
                        Status = recording.Status,
                        UploadedChunks = recording.UploadedChunks,
                        ExpectedChunks = recording.ExpectedChunks,
                        MissingChunks = new List<int>(),
                        StoppedAt = recording.StoppedAt,
                        Message = "Recording is already finalizing."
                    };

                    return Response<StopRecordingResponse>.Success(alreadyFinalizingResponse,"Recording is already finalizing.",200);
                }

                if (request.ExpectedChunks.HasValue)
                {
                    if (recording.ExpectedChunks.HasValue &&
                        recording.ExpectedChunks.Value != request.ExpectedChunks.Value)
                    {
                        return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"ExpectedChunks does not match the previously registered value.",400);
                    }

                    recording.SetExpectedChunks(request.ExpectedChunks.Value);
                }

                if (!recording.ExpectedChunks.HasValue)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"ExpectedChunks is required to finalize a chunked recording.",400);
                }

                var uploadedChunkNumbers = await _context.RecordingChunks
                    .AsNoTracking()
                    .Where(c => c.RecordingId == recording.Id && c.Status == RecordingChunkStatus.Uploaded)
                    .Select(c => c.ChunkNumber)
                    .ToListAsync(cancellationToken);

                var uploadedChunkSet = uploadedChunkNumbers.ToHashSet();

                var missingChunks = Enumerable
                    .Range(0, recording.ExpectedChunks.Value)
                    .Where(chunkNumber => !uploadedChunkSet.Contains(chunkNumber))
                    .ToList();

                if (missingChunks.Any())
                {
                    var incompleteResponse = new StopRecordingResponse
                    {
                        RecordingId = recording.Id,
                        Status = recording.Status,
                        UploadedChunks = recording.UploadedChunks,
                        ExpectedChunks = recording.ExpectedChunks,
                        MissingChunks = missingChunks,
                        StoppedAt = recording.StoppedAt,
                        Message = "Recording cannot be finalized because some chunks are missing."
                    };

                    return Response<StopRecordingResponse>.Failure(incompleteResponse,"Missing chunks detected.",409,missingChunks.Select(x => $"Missing chunk: {x}").ToList());
                }

                recording.MarkFinalizing();

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(), "A concurrency error occurred while finalizing the recording.", 409, new List<string> { ex.Message });

                }
                catch (DbUpdateException ex)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"A database error occurred while stopping the recording.",500,new List<string> { ex.Message });
                }

                await _backgroundJobService.EnqueueFinalizeRecordingAsync(recording.Id, cancellationToken);

                var response = new StopRecordingResponse
                {
                    RecordingId = recording.Id,
                    Status = recording.Status,
                    UploadedChunks = recording.UploadedChunks,
                    ExpectedChunks = recording.ExpectedChunks,
                    MissingChunks = new List<int>(),
                    StoppedAt = recording.StoppedAt,
                    Message = "Recording stopped successfully. Finalization has started."
                };

                return Response<StopRecordingResponse>.Success(response,"Recording stopped successfully.",200);
            }
            catch (Exception ex)
            {
                return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"An unexpected error occurred while stopping the recording.",500,new List<string> { ex.Message });
            }
        }

        private static List<string> ValidateStartRequest(StartRecordingRequest request)
        {
            var errors = new List<string>();

            if (request.MeetingId == Guid.Empty)
                errors.Add("MeetingId is required.");

            if (string.IsNullOrWhiteSpace(request.CreatedById))
                errors.Add("CreatedById is required.");

            if (string.IsNullOrWhiteSpace(request.FileName))
                errors.Add("FileName is required.");

            if (request.ExpectedChunks.HasValue && request.ExpectedChunks < 0)
                errors.Add("ExpectedChunks cannot be negative.");

            if (request.UploadMode == RecordingUploadMode.Chunked && request.ExpectedChunks == 0)
                errors.Add("ExpectedChunks cannot be zero for chunked upload when provided.");

            return errors;
        }

        private static List<string> ValidateChunkRequest(UploadChunkRequest request)
        {
            var errors = new List<string>();

            if (request.RecordingId == Guid.Empty)
                errors.Add("RecordingId is required.");

            if (request.ChunkNumber < 0)
                errors.Add("ChunkNumber must be zero or greater.");

            if (request.ChunkStream is null)
                errors.Add("ChunkStream is required.");

            if (request.Size <= 0)
                errors.Add("Chunk size must be greater than zero.");

            if (string.IsNullOrWhiteSpace(request.FileName))
                errors.Add("Chunk file name is required.");

            if (string.IsNullOrWhiteSpace(request.ContentType))
                errors.Add("Chunk content type is required.");

            return errors;
        }

        private static List<string> ValidateFileUploadRequest(UploadRecordingFileRequest request)
        {
            var errors = new List<string>();

            if (request.RecordingId == Guid.Empty)
                errors.Add("RecordingId is required.");

            if (request.File is null)
                errors.Add("File is required.");
            else if (request.File.Length <= 0)
                errors.Add("File is empty.");

            return errors;
        }

        private static List<string> ValidateStopRequest(StopRecordingRequest request)
        {
            var errors = new List<string>();

            if (request.RecordingId == Guid.Empty)
                errors.Add("RecordingId is required.");

            if (request.ExpectedChunks.HasValue && request.ExpectedChunks <= 0)
                errors.Add("ExpectedChunks must be greater than zero when provided.");

            return errors;
        }

        private static List<string> ValidateRecordingCanAcceptChunk(Recording recording, string requestContentType)
        {
            var errors = new List<string>();

            if (recording.Status == RecordingStatus.Completed)
                errors.Add("Recording is already completed.");

            if (recording.Status == RecordingStatus.Failed)
                errors.Add("Recording is marked as failed.");

            if (recording.Status == RecordingStatus.Expired)
                errors.Add("Recording is expired.");

            if (recording.Status == RecordingStatus.Ending)
                errors.Add("Recording is finalizing and cannot accept more chunks.");

            if (!string.IsNullOrWhiteSpace(recording.ContentType) &&
                !string.Equals(recording.ContentType, requestContentType, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Chunk content type does not match recording content type.");
            }

            return errors;
        }

        private static Response<UploadChunkResponse> BuildDuplicateChunkResponse(Recording recording,RecordingChunk existingChunk,UploadChunkRequest request)
        {
            var sameChecksum =
                !string.IsNullOrWhiteSpace(request.Checksum) &&
                !string.IsNullOrWhiteSpace(existingChunk.Checksum) &&
                string.Equals(request.Checksum, existingChunk.Checksum, StringComparison.OrdinalIgnoreCase);

            var sameSize = existingChunk.Size == request.Size;

            var canTreatAsIdempotentSuccess =
                sameChecksum || (string.IsNullOrWhiteSpace(request.Checksum) && sameSize);

            if (!canTreatAsIdempotentSuccess)
            {
                return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Chunk number already exists with different content.",409,new List<string> { $"Chunk number {request.ChunkNumber} already exists with different content." });
            }

            var response = new UploadChunkResponse
            {
                RecordingId = recording.Id,
                ChunkId = existingChunk.Id,
                ChunkNumber = existingChunk.ChunkNumber,
                ChunkStatus = existingChunk.Status,
                RecordingStatus = recording.Status,
                IsDuplicate = true,
                UploadedChunks = recording.UploadedChunks,
                ExpectedChunks = recording.ExpectedChunks,
                ReceivedBytes = recording.ReceivedBytes,
                UploadedAt = existingChunk.UploadedAt
            };

            return Response<UploadChunkResponse>.Success(response,"Duplicate chunk detected; existing chunk returned.",200);
        }

        //private static IFormFile BuildFormFile(Stream stream, long size, string fileName, string formFieldName)
        //{
        //    if (stream.CanSeek)
        //        stream.Position = 0;

        //    return new FormFile(stream, 0, size, formFieldName, fileName)
        //    {
        //        Headers = new HeaderDictionary(),
        //        ContentType = GetContentTypeFromFileName(fileName)
        //    };
        //}

        //private static string GetContentTypeFromFileName(string fileName)
        //{
        //    var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

        //    return extension switch
        //    {
        //        ".webm" => "audio/webm",
        //        ".wav" => "audio/wav",
        //        ".mp3" => "audio/mpeg",
        //        ".mp4" => "audio/mp4",
        //        ".m4a" => "audio/mp4",
        //        ".ogg" => "audio/ogg",
        //        _ => "application/octet-stream"
        //    };
        //}

        private async Task SafeDeleteChunkFromCloudAsync(string? publicId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return;

            try
            {
                await _cloudinaryService.DeleteFileAsync(publicId, cancellationToken);
            }
            catch
            {
                // swallow intentionally; orphan cleanup can be handled later
            }
        }

        private static bool IsChunkUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is PostgresException postgresException &&
                   postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                   postgresException.ConstraintName == ChunkUniqueConstraintName;
        }

        private static bool IsActiveRecordingUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is PostgresException postgresException &&
                   postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                   postgresException.ConstraintName == ActiveRecordingConstraintName;
        }
    }
}
