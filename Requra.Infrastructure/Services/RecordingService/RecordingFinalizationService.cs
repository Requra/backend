using Microsoft.EntityFrameworkCore;
using Requra.Application.Interfaces.IRecordingService;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Services.RecordingService
{
    public class RecordingFinalizationService : IRecordingFinalizationService
    {
        private readonly RequraDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IRecordingChunkStorageReader _chunkStorageReader;

        public RecordingFinalizationService(RequraDbContext context,ICloudinaryService cloudinaryService,IRecordingChunkStorageReader chunkStorageReader)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _chunkStorageReader = chunkStorageReader;
        }

        public async Task FinalizeRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default)
        {
            var recording = await _context.Recordings
                .Include(r => r.Meeting)
                .FirstOrDefaultAsync(r => r.Id == recordingId, cancellationToken);

            if (recording is null)
                throw new InvalidOperationException("Recording not found.");

            if (recording.Status != RecordingStatus.FINALIZING)
                throw new InvalidOperationException("Recording is not in ending state.");

            var chunks = await _context.RecordingChunks
                .AsNoTracking()
                .Where(c => c.RecordingId == recordingId && c.Status == RecordingChunkStatus.Uploaded)
                .OrderBy(c => c.ChunkNumber)
                .ToListAsync(cancellationToken);

            if (!recording.ExpectedChunks.HasValue)
            {
                recording.MarkFailed("Expected chunk count is missing.", "Cannot finalize without ExpectedChunks.");
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            if (chunks.Count != recording.ExpectedChunks.Value)
            {
                var uploadedSet = chunks.Select(c => c.ChunkNumber).ToHashSet();

                var missing = Enumerable.Range(0, recording.ExpectedChunks.Value)
                    .Where(x => !uploadedSet.Contains(x))
                    .ToList();

                recording.MarkFailed(
                    "Missing chunks.",
                    $"Missing chunks during finalization: {string.Join(",", missing)}");

                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{recording.Id}-{Guid.NewGuid()}.webm");

            try
            {
                await using (var output = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    foreach (var chunk in chunks)
                    {
                        await using var chunkStream = await _chunkStorageReader.OpenReadAsync(
                            chunk.StorageUrl,
                            chunk.PublicId,
                            cancellationToken);

                        await chunkStream.CopyToAsync(output, cancellationToken);
                    }
                }

                await using var finalStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                if (finalStream.CanSeek)
                    finalStream.Position = 0;

                var finalFileSize = finalStream.Length;

                var uploadResult = await _cloudinaryService.UploadStreamAsync(
                    finalStream,
                    fileName: recording.FileName,
                    folderName: $"recordings/{recording.MeetingId}/{recording.Id}",
                    contentType: recording.ContentType,
                    cancellationToken: cancellationToken,
                    publicId: "recording-final",
                    overwrite: true);

                if (!uploadResult.IsSuccess || string.IsNullOrWhiteSpace(uploadResult.PublicId) || string.IsNullOrWhiteSpace(uploadResult.Url))
                {
                    recording.MarkFailed(
                        "Final file upload failed.",
                        uploadResult.ErrorMessage ?? "Cloudinary upload failed.");

                    await _context.SaveChangesAsync(cancellationToken);
                    return;
                }

                recording.SetStorage(
                    uploadResult.Url,
                    uploadResult.PublicId,
                    uploadResult.PublicId,
                    finalFileSize);
                //recording.SetStorage(
                //    uploadResult.Url!,
                //    uploadResult.PublicId!,
                //    uploadResult.PublicId!,
                //    finalStream.Length);

                recording.MarkCompleted();

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                recording.MarkFailed("Finalization failed.", ex.Message);
                await _context.SaveChangesAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
