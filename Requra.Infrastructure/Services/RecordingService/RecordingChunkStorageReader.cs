using Requra.Application.Interfaces.IRecordingService;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Services.RecordingService
{
    internal class RecordingChunkStorageReader : IRecordingChunkStorageReader
    {
        private readonly ICloudinaryService _cloudinaryService;

        public RecordingChunkStorageReader(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        public async Task<Stream> OpenReadAsync(string storageUrl,string? publicId,CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(publicId))
            {
                return await _cloudinaryService.DownloadFileAsync(
                    publicId,
                    resourceType: "raw",
                    cancellationToken: cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(storageUrl))
                throw new ArgumentException("Either storageUrl or publicId must be provided.");

            using var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(
                storageUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Failed to download chunk from storage URL. Status code: {response.StatusCode}");
            }

            var memoryStream = new MemoryStream();

            await using var remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await remoteStream.CopyToAsync(memoryStream, cancellationToken);

            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
