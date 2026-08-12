using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Requra.Application.DTOs.Recordings;
using Requra.Application.Interfaces.IRecordingService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;

namespace Requra.Infrastructure.Services.RecordingService
{
    public class RecordingService(RequraDbContext _context, ICloudinaryService _cloudinaryService, IRecordingBackgroundJobService _backgroundJobService) : IRecordingService
    {
        private const string ActiveRecordingConstraintName = "ux_recordings_meeting_id_one_active";
        private const string ChunkUniqueConstraintName = "ux_recording_chunks_recording_id_chunk_number";

        public async Task<Response<GetRecordingStatusResponse>> GetRecordingStatusAsync(Guid recordingId,CancellationToken cancellationToken = default)
        {
            if (recordingId == Guid.Empty)
            {
                return Response<GetRecordingStatusResponse>.Failure(new GetRecordingStatusResponse(),"RecordingId is required.",400,new List<string> { "RecordingId is required." });
            }

            try
            {
                var recording = await _context.Recordings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == recordingId, cancellationToken);

                if (recording is null)
                {
                    return Response<GetRecordingStatusResponse>.Failure(new GetRecordingStatusResponse(),"Recording not found.",404);
                }

                var uploadedChunkIndexes = await _context.RecordingChunks
                    .AsNoTracking()
                    .Where(c => c.RecordingId == recordingId && c.Status == RecordingChunkStatus.Uploaded)
                    .OrderBy(c => c.ChunkNumber)
                    .Select(c => c.ChunkNumber)
                    .ToListAsync(cancellationToken);

                var missingChunkIndexes = new List<int>();

                if (recording.UploadMode == RecordingUploadMode.Chunked &&
                    recording.ExpectedChunks.HasValue &&
                    recording.ExpectedChunks.Value > 0)
                {
                    var uploadedSet = uploadedChunkIndexes.ToHashSet();

                    missingChunkIndexes = Enumerable
                        .Range(0, recording.ExpectedChunks.Value)
                        .Where(index => !uploadedSet.Contains(index))
                        .ToList();
                }

                int? durationSeconds = null;

                if (recording.CompletedAt.HasValue)
                {
                    durationSeconds = (int)Math.Max(
                        0,
                        Math.Round((recording.CompletedAt.Value - recording.StartedAt).TotalSeconds));
                }
                else if (recording.Status == RecordingStatus.ACTIVE ||
                         recording.Status == RecordingStatus.READY )
                {
                    durationSeconds = (int)Math.Max(0,Math.Round((DateTime.UtcNow - recording.StartedAt).TotalSeconds));
                }

                var response = new GetRecordingStatusResponse
                {
                    Id = recording.Id,
                    MeetingId = recording.MeetingId,
                    Status = recording.Status,
                    UploadMode = recording.UploadMode,
                    MimeType = recording.ContentType,
                    FileUrl = recording.StorageUrl,
                    DurationSeconds = durationSeconds,
                    ChunksCount = uploadedChunkIndexes.Count,
                    MissingChunkIndexes = missingChunkIndexes,
                    CreatedAt = recording.CreatedAt,
                    CompletedAt = recording.CompletedAt,
                    DocumentId = null
                };

                return Response<GetRecordingStatusResponse>.Success(response,"Recording status retrieved successfully.",200);
            }
            catch (Exception ex)
            {
                return Response<GetRecordingStatusResponse>.Failure(new GetRecordingStatusResponse(),"An unexpected error occurred while retrieving recording status.",500,new List<string> { ex.Message });
            }
        }
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
                        (r.Status == RecordingStatus.READY ||
                         r.Status == RecordingStatus.ACTIVE ||
                         r.Status == RecordingStatus.FINALIZING),
                        cancellationToken);

                if (hasActiveRecording)
                {
                    return Response<StartRecordingResponse>.Failure(new StartRecordingResponse(), "An active recording already exists for this meeting.", 409);
                }

                var normalizedMimeType = request.MimeType
                            .Split(';', 2)[0]
                            .Trim()
                            .ToLowerInvariant();
                string extension = GetExtensionFromContentType(request.MimeType);

                var recording = new Recording(request.MeetingId, request.CreatedById, request.UploadMode, request.MimeType, extension,0);

                var finalPublicId = $"recording-final";
                var folder = $"recordings/{recording.MeetingId}/{recording.Id}";
                var finalFileName = $"{recording.FileName}.{extension}";
                var predictedUrl = _cloudinaryService.GetFileUrl($"{folder}/{finalPublicId}", "video");

                recording.SetPlannedStorage(predictedUrl,$"{folder}/{finalPublicId}",finalPublicId);
                recording.MarkActive();
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
                    Id = recording.Id,
                    MeetingId = recording.MeetingId,
                    FileUrl = recording.StorageUrl,
                    MimeType = recording.ContentType,
                    UploadMode = recording.UploadMode,
                    Status = recording.Status,
                    CreatedAt = recording.CreatedAt

                };

                return Response<StartRecordingResponse>.Success(response, "Recording session started successfully", 201);
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
            {
                return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Validation failed.",400,validationErrors);
            }

            try
            {
                var recording = await _context.Recordings
                    .FirstOrDefaultAsync(r => r.Id == request.RecordingId, cancellationToken);

                if (recording is null)
                {
                    return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Recording not found.",404);
                }

                var requestContentType = string.IsNullOrWhiteSpace(request.AudioChunk.ContentType)? "application/octet-stream": request.AudioChunk.ContentType.Split(';', 2)[0].Trim().ToLowerInvariant(); ;

                var stateErrors = ValidateRecordingCanAcceptChunk(recording, requestContentType);
                if (stateErrors.Any())
                {
                    return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Recording cannot accept this chunk.",409,stateErrors);
                }

                if (recording.UploadMode != RecordingUploadMode.Chunked)
                {
                    return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"This recording does not accept chunk uploads.",400);
                }
                if (!request.ChunkIndex.HasValue ||
                    request.ChunkIndex.Value < 0)
                {
                    return Response<UploadChunkResponse>.Failure(
                        new UploadChunkResponse(),
                        "ChunkIndex must be greater than or equal to zero.",
                        StatusCodes.Status400BadRequest);
                }

                var existingChunk = await _context.RecordingChunks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c => c.RecordingId == request.RecordingId && c.ChunkNumber == request.ChunkIndex.Value,
                        cancellationToken);

                if (existingChunk is not null)
                {
                    return BuildDuplicateChunkResponse(recording, existingChunk, request);
                }

                var uploadResult = await _cloudinaryService.UploadFileAsync(
                    request.AudioChunk,
                    folderName: $"recordings/{recording.MeetingId}/{recording.Id}/chunks",
                    cancellationToken: cancellationToken,
                    publicId: $"chunk-{request.ChunkIndex}",
                    overwrite: false);

                if (!uploadResult.IsSuccess ||
                    string.IsNullOrWhiteSpace(uploadResult.PublicId) ||
                    string.IsNullOrWhiteSpace(uploadResult.Url))
                {
                    return Response<UploadChunkResponse>.Failure( new UploadChunkResponse(), "Failed to upload chunk to cloud storage.", 500, new List<string> { uploadResult.ErrorMessage ?? "Unknown cloud upload error." });
                }

                var chunk = new RecordingChunk(
                    request.RecordingId,
                    request.ChunkIndex ?? 0,
                    uploadResult.Url!,
                    request.AudioChunk.Length,
                    uploadResult.PublicId,
                    uploadResult.PublicId,
                    request.Checksum,
                    requestContentType,
                    request.StartedAtMs,
                    request.EndedAtMs);

                chunk.SetTimeRange(request.StartedAtMs, request.EndedAtMs);

                await _context.RecordingChunks.AddAsync(chunk, cancellationToken);

                recording.MarkUploading();
                recording.RegisterChunk(request.AudioChunk.Length);

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
                        .FirstOrDefaultAsync(
                            c => c.RecordingId == request.RecordingId && c.ChunkNumber == request.ChunkIndex,
                            cancellationToken);

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
                    ChunkIndex = chunk.ChunkNumber,
                    Status = chunk.Status,
                    SizeBytes = chunk.Size,
                    StartedAtMs = chunk.StartedAtMs,
                    EndedAtMs = chunk.EndedAtMs,
                    UploadedAt = chunk.UploadedAt
                };

                return Response<UploadChunkResponse>.Success(response,"Recording chunk uploaded successfully.",200);
            }
            catch (Exception ex)
            {
                return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"An unexpected error occurred while uploading the chunk.",500,new List<string> { ex.Message });
            }
        }

        #region StreamingVersion
        //public async Task<Response<UploadChunkResponse>> UploadChunkAsync(UploadChunkRequest request,CancellationToken cancellationToken = default)
        //{
        //    var validationErrors = ValidateChunkRequest(request);
        //    if (validationErrors.Any())
        //        return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Validation failed.",400,validationErrors);

        //    try
        //    {
        //        var recording = await _context.Recordings
        //            .FirstOrDefaultAsync(r => r.Id == request.RecordingId, cancellationToken);

        //        if (recording is null)
        //        {
        //            return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Recording not found.",404);
        //        }

        //        var stateErrors = ValidateRecordingCanAcceptChunk(recording, request.ContentType);
        //        if (stateErrors.Any())
        //        {
        //            return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Recording cannot accept this chunk.",409,stateErrors);
        //        }

        //        if (recording.UploadMode != RecordingUploadMode.Chunked)
        //        {
        //            return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"This recording does not accept chunk uploads.",400);
        //        }

        //        var existingChunk = await _context.RecordingChunks
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync(c => c.RecordingId == request.RecordingId && c.ChunkNumber == request.ChunkNumber,cancellationToken);

        //        if (existingChunk is not null)
        //        {
        //            var duplicateResponse = BuildDuplicateChunkResponse(recording, existingChunk, request);
        //            if (!duplicateResponse.IsSuccess)
        //                return duplicateResponse;

        //            return duplicateResponse;
        //        }

        //        if (request.ChunkStream.CanSeek)
        //            request.ChunkStream.Position = 0;

        //        var uploadResult = await _cloudinaryService.UploadStreamAsync(
        //            request.ChunkStream,
        //            fileName: request.FileName,
        //            folderName: $"recordings/{recording.MeetingId}/{recording.Id}/chunks",
        //            contentType: request.ContentType,
        //            cancellationToken: cancellationToken,
        //            publicId: $"chunk-{request.ChunkNumber}",
        //            overwrite: false);

        //        if (!uploadResult.IsSuccess || string.IsNullOrWhiteSpace(uploadResult.PublicId) || string.IsNullOrWhiteSpace(uploadResult.Url))
        //        {
        //            return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Failed to upload chunk to cloud storage.",500,new List<string> { uploadResult.ErrorMessage ?? "Unknown cloud upload error." });
        //        }

        //        var chunk = new RecordingChunk(
        //            request.RecordingId,
        //            request.ChunkNumber,
        //            uploadResult.Url!,
        //            request.Size,
        //            uploadResult.PublicId,
        //            uploadResult.PublicId,
        //            request.Checksum,
        //            request.ContentType);

        //        await _context.RecordingChunks.AddAsync(chunk, cancellationToken);

        //        recording.MarkUploading();
        //        recording.RegisterChunk(request.Size);

        //        try
        //        {
        //            await _context.SaveChangesAsync(cancellationToken);
        //        }
        //        catch (DbUpdateConcurrencyException ex)
        //        {
        //            await SafeDeleteChunkFromCloudAsync(uploadResult.PublicId, cancellationToken);

        //            return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"A concurrency conflict occurred while saving the uploaded chunk.",409,new List<string> { ex.Message });
        //        }
        //        catch (DbUpdateException ex) when (IsChunkUniqueViolation(ex))
        //        {
        //            await SafeDeleteChunkFromCloudAsync(uploadResult.PublicId, cancellationToken);

        //            var duplicateChunk = await _context.RecordingChunks
        //                .AsNoTracking()
        //                .FirstOrDefaultAsync(c => c.RecordingId == request.RecordingId && c.ChunkNumber == request.ChunkNumber,cancellationToken);

        //            if (duplicateChunk is null)
        //            {
        //                return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"Chunk upload conflict occurred.",409);
        //            }

        //            return BuildDuplicateChunkResponse(recording, duplicateChunk, request);
        //        }
        //        catch (DbUpdateException ex)
        //        {
        //            await SafeDeleteChunkFromCloudAsync(uploadResult.PublicId, cancellationToken);

        //            return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"A database error occurred while saving the uploaded chunk.",500,new List<string> { ex.Message });
        //        }

        //        var response = new UploadChunkResponse
        //        {
        //            RecordingId = recording.Id,
        //            ChunkId = chunk.Id,
        //            ChunkNumber = chunk.ChunkNumber,
        //            ChunkStatus = chunk.Status,
        //            RecordingStatus = recording.Status,
        //            IsDuplicate = false,
        //            UploadedChunks = recording.UploadedChunks,
        //            ExpectedChunks = recording.ExpectedChunks,
        //            ReceivedBytes = recording.ReceivedBytes,
        //            UploadedAt = chunk.UploadedAt
        //        };

        //        return Response<UploadChunkResponse>.Success(response,"Chunk uploaded successfully.",200);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Response<UploadChunkResponse>.Failure(new UploadChunkResponse(),"An unexpected error occurred while uploading the chunk.",500,new List<string> { ex.Message });
        //    }
        //} 
        #endregion

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

                if (recording.Status == RecordingStatus.STOPPED)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Recording is already completed.",409);
                }

                if (recording.Status == RecordingStatus.FINALIZING)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Recording is currently ending.",409);
                }

                if (recording.Status == RecordingStatus.FAILED)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Recording is marked as failed.",409);
                }

                if (recording.Status == RecordingStatus.EXPIRED)
                {
                    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(),"Recording is expired.",409);
                }

                //if (!string.IsNullOrWhiteSpace(recording.ContentType) &&!string.Equals(recording.ContentType, request.File.ContentType, StringComparison.OrdinalIgnoreCase))
                //{
                //    return Response<UploadRecordingFileResponse>.Failure(new UploadRecordingFileResponse(), $"Uploaded file content type does not match recording content type. Expected '{recording.ContentType}', got '{request.File.ContentType}'.", 400);
                //}
                var extension = Path.GetExtension(request.File.FileName)?.ToLowerInvariant();
                //var contentType = GetContentTypeFromFileName(request.File.FileName) ?? request.File.ContentType;
                var contentType =
           !string.IsNullOrWhiteSpace(request.File.ContentType)
               ? request.File.ContentType
                   .Split(';', 2)[0]
                   .Trim()
                   .ToLowerInvariant()
               : GetContentTypeFromFileName(request.File.FileName);

                if (!string.IsNullOrWhiteSpace(extension))
                {
                    recording.SetOriginalExtension(extension);
                }

                if (!string.IsNullOrWhiteSpace(contentType))
                {
                    recording.SetContentType(contentType);
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
                    Id = recording.Id,
                    MeetingId = recording.MeetingId,
                    FileUrl = recording.StorageUrl,
                    MimeType = recording.ContentType,
                    UploadMode = recording.UploadMode,
                    Status = recording.Status,
                    CreatedAt = recording.CreatedAt,
                    CompletedAt= recording.CompletedAt,
                    DurationSeconds =request.durationSeconds
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

                //if (recording.UploadMode != RecordingUploadMode.Chunked)
                //{
                //    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"Stop endpoint is only valid for chunked recordings.",400);
                //}

                if (recording.Status == RecordingStatus.STOPPED)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"Recording is already completed.",409);
                }

                if (recording.Status == RecordingStatus.FAILED)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"Recording is marked as failed.",409);
                }

                if (recording.Status == RecordingStatus.EXPIRED)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"Recording is expired.",409);
                }

                //if (recording.Status == RecordingStatus.ACTIVE)
                //{
                //    var alreadyFinalizingResponse = new StopRecordingResponse
                //    {
                //        Id = recording.Id,
                //        MeetingId = recording.MeetingId,
                //        Status = recording.Status,
                //        MimeType = recording.ContentType,
                //        UploadMode = recording.UploadMode,
                //        chunksCount = recording.UploadedChunks,
                //        MissingChunkIndexes = new List<int>(),
                //        CompletedAt = DateTime.UtcNow,
                //        CreatedAt = recording.CreatedAt,
                //        FileUrl = recording.StorageUrl,
                //        DurationSeconds=request.DurationSeconds,
                //    };
                //    recording.MarkFinalizing();

                //    return Response<StopRecordingResponse>.Success(alreadyFinalizingResponse,"Recording is already finalizing.",200);
                //}

                if (request.lastChunkIndex.HasValue)
                {
                    var expectedChunks = request.lastChunkIndex.Value + 1;
                    if (recording.ExpectedChunks.HasValue &&
                  recording.ExpectedChunks.Value != expectedChunks)
                    {
                        return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(),"ExpectedChunks does not match the previously registered value.",StatusCodes.Status400BadRequest);
                    }

                    recording.SetExpectedChunks(expectedChunks);
                }

                if (!recording.ExpectedChunks.HasValue)
                {
                    return Response<StopRecordingResponse>.Failure(new StopRecordingResponse(), "LastChunkIndex is required to finalize a chunked recording.", 400);
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
                        Id = recording.Id,
                        MeetingId = recording.MeetingId,
                        Status = recording.Status,
                        MimeType = recording.ContentType,
                        UploadMode = recording.UploadMode,
                        chunksCount = recording.UploadedChunks,
                        MissingChunkIndexes = missingChunks,
                        CompletedAt = DateTime.UtcNow,
                        CreatedAt = recording.CreatedAt,
                        FileUrl = recording.StorageUrl,
                        DurationSeconds = request.DurationSeconds,
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
                    Id = recording.Id,
                    MeetingId = recording.MeetingId,
                    Status = recording.Status,
                    MimeType = recording.ContentType,
                    UploadMode = recording.UploadMode,
                    chunksCount = recording.UploadedChunks,
                    MissingChunkIndexes = new List<int>(),
                    CompletedAt = DateTime.UtcNow,
                    CreatedAt = recording.CreatedAt,
                    FileUrl = recording.StorageUrl,
                    DurationSeconds = request.DurationSeconds,
                };

                return Response<StopRecordingResponse>.Success(response, "Recording finalization started successfully.", 200);
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
            if (string.IsNullOrWhiteSpace(request.MimeType))
            {
                errors.Add("MimeType is required.");
            }
            else
            {
                var mimeType = request.MimeType
                    .Split(';', 2)[0]
                    .Trim()
                    .ToLowerInvariant();

                if (mimeType != "audio/webm")
                {
                    errors.Add(
                        "MimeType must be audio/webm.");
                }
            }

            //if (request.UploadMode != RecordingUploadMode.Chunked )
            //    errors.Add("ExpectedUploadMode is invalid.");

            return errors;
        }

        private static List<string> ValidateChunkRequest(UploadChunkRequest request)
        {
            var errors = new List<string>();

            if (request.RecordingId == Guid.Empty)
                errors.Add("RecordingId is required.");

            if (request.ChunkIndex < 0)
                errors.Add("ChunkIndex must be zero or greater.");

            if (request.AudioChunk is null)
                errors.Add("AudioChunk is required.");
            else
            {
                if (request.AudioChunk.Length <= 0)
                    errors.Add("AudioChunk is empty.");
            }

            if (request.StartedAtMs.HasValue && request.StartedAtMs.Value < 0)
                errors.Add("StartedAtMs must be zero or greater.");

            if (request.EndedAtMs.HasValue && request.EndedAtMs.Value < 0)
                errors.Add("EndedAtMs must be zero or greater.");

            if (request.StartedAtMs.HasValue &&
                request.EndedAtMs.HasValue &&
                request.EndedAtMs.Value < request.StartedAtMs.Value)
            {
                errors.Add("EndedAtMs must be greater than or equal to StartedAtMs.");
            }

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

            if (request.durationSeconds.HasValue && request.durationSeconds <= 0)
                errors.Add("DurationSeconds must be greater than zero when provided.");

            return errors;
        }

        private static List<string> ValidateStopRequest(StopRecordingRequest request)
        {
            var errors = new List<string>();

            if (request.RecordingId == Guid.Empty)
                errors.Add("RecordingId is required.");
            if (request.DurationSeconds <= 0)
                errors.Add("DurationSeconds must be greater than zero.");

            if (request.lastChunkIndex.HasValue && request.lastChunkIndex < 0)
                errors.Add("LastChunkIndex must be greater than zero when provided.");

            return errors;
        }

        private static List<string> ValidateRecordingCanAcceptChunk(Recording recording, string requestContentType)
        {
            var errors = new List<string>();

            if (recording.Status == RecordingStatus.STOPPED)
                errors.Add("Recording is already stopped.");

            if (recording.Status == RecordingStatus.FAILED)
                errors.Add("Recording is marked as failed.");

            if (recording.Status == RecordingStatus.EXPIRED)
                errors.Add("Recording is expired.");

            if (recording.Status == RecordingStatus.FINALIZING)
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

            var sameSize = existingChunk.Size == request.AudioChunk.Length;

            var sameStartedAt =
                (!request.StartedAtMs.HasValue && !existingChunk.StartedAtMs.HasValue) ||
                request.StartedAtMs == existingChunk.StartedAtMs;

            var sameEndedAt =
                (!request.EndedAtMs.HasValue && !existingChunk.EndedAtMs.HasValue) ||
                request.EndedAtMs == existingChunk.EndedAtMs;

            var canTreatAsIdempotentSuccess =
                sameChecksum ||
                (string.IsNullOrWhiteSpace(request.Checksum) && sameSize && sameStartedAt && sameEndedAt);

            if (!canTreatAsIdempotentSuccess)
            {
                return Response<UploadChunkResponse>.Failure(
                    new UploadChunkResponse(),
                    "Chunk index already exists with different content.",
                    409,
                    new List<string> { $"Chunk index {request.ChunkIndex} already exists with different content." });
            }

            var response = new UploadChunkResponse
            {
                RecordingId = recording.Id,
                ChunkIndex = existingChunk.ChunkNumber,
                Status = existingChunk.Status,
                SizeBytes = existingChunk.Size,
                StartedAtMs = existingChunk.StartedAtMs,
                EndedAtMs = existingChunk.EndedAtMs,
                UploadedAt = existingChunk.UploadedAt
            };

            return Response<UploadChunkResponse>.Success(
                response,
                "Duplicate chunk detected; existing chunk returned.",
                200);
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

        private static string GetContentTypeFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

            return extension switch
            {
                ".webm" => "audio/webm",
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".mp4" => "audio/mp4",
                ".m4a" => "audio/mp4",
                ".ogg" => "audio/ogg",
                _ => "application/octet-stream"
            };
        }
        private static string? GetExtensionFromContentType(string? contentType)
        {
            var mimeType = contentType
                    .Split(';', 2)[0]
                    .Trim()
                    .ToLowerInvariant();

            return mimeType switch
            {
                "audio/webm" => ".webm",
                "audio/wav" => ".wav",
                "audio/mpeg" => ".mp3",
                "audio/mp4" => ".m4a",
                "audio/ogg" => ".ogg",
                _ => null
            };
        }
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
