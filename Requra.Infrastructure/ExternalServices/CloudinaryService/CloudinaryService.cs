using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Requra.Infrastructure.ExternalDTOs.CloudinaryDto;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;
using System.Net;

namespace Requra.Infrastructure.ExternalServices.CloudinaryService
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        private static readonly string[] ImageExtensions =
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp"
        };

        private static readonly string[] VideoExtensions =
        {
            ".mp4", ".mov", ".avi", ".mkv", ".webm"
        };

        private static readonly string[] AudioExtensions =
        {
            ".mp3", ".wav", ".aac", ".flac", ".ogg", ".m4a"
        };

        public CloudinaryService(IConfiguration configuration,ILogger<CloudinaryService> logger)
        {
            _logger = logger;

            var account = new Account
            {
                Cloud = configuration["Cloudinary:CloudName"],
                ApiKey = configuration["Cloudinary:ApiKey"],
                ApiSecret = configuration["Cloudinary:ApiSecret"]
            };

            _cloudinary = new Cloudinary(account);
        }

        public string GetFileUrl(string publicId, string resourceType = "image")
        {
            if (string.IsNullOrWhiteSpace(publicId))
                throw new ArgumentException("PublicId is required.", nameof(publicId));

            return resourceType.ToLowerInvariant() switch
            {
                "image" => _cloudinary.Api.UrlImgUp.BuildUrl(publicId),
                "video" => _cloudinary.Api.UrlVideoUp.BuildUrl(publicId),
                "raw" => _cloudinary.Api.UrlImgUp.ResourceType("raw").BuildUrl(publicId),
                _ => _cloudinary.Api.UrlImgUp.BuildUrl(publicId)
            };
        }

        public async Task<UploadResultDto> UploadStreamAsync(Stream stream,string fileName,string folderName = "general",string? contentType = null,CancellationToken cancellationToken = default,string? publicId = null,bool overwrite = false)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable.", nameof(stream));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required.", nameof(fileName));

            long size = 0;
            if (stream.CanSeek)
            {
                size = stream.Length;
                stream.Position = 0;
            }

            return await UploadInternalAsync(stream,fileName,contentType,size,folderName,cancellationToken,publicId,overwrite);
        }

        public async Task<UploadResultDto> UploadFileAsync(IFormFile file, string folderName = "general", CancellationToken cancellationToken = default, string? publicId = null, bool overwrite = false)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            cancellationToken.ThrowIfCancellationRequested();

            var originalFileName = file.FileName;
            var extension = Path.GetExtension(originalFileName).ToLower();

            var fileName = !string.IsNullOrWhiteSpace(originalFileName)
                ? Path.GetFileNameWithoutExtension(originalFileName)
                : Guid.NewGuid().ToString();

            await using var stream = file.OpenReadStream();

            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp" };
            var videoExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
            var audioExtensions = new[] { ".mp3", ".wav", ".aac", ".flac", ".ogg", ".m4a" };

            bool isImage = imageExtensions.Contains(extension);
            bool isVideo = videoExtensions.Contains(extension);
            bool isAudio = audioExtensions.Contains(extension);

            long maxSize = isImage ? 10_000_000 // 10 MB
                          : isVideo ? 100_000_000 // 100 MB
                          : isAudio ? 100_000_000 // 100 MB
                          : 10_000_000; // raw

            if (file.Length > maxSize)
                throw new Exception($"File exceeds allowed size for {extension}");

            UploadResult uploadResult;

            if (isImage)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName + extension, stream),
                    Folder = folderName,
                    PublicId = publicId,
                    Overwrite = overwrite,
                    UseFilename = publicId == null,
                    UniqueFilename = publicId == null,

                    //Optimization
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto")
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            }
            else if (isVideo | isAudio)
            {
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(fileName + extension, stream),
                    Folder = folderName,
                    UseFilename = true,
                    UniqueFilename = true,

                    //optimization
                    Transformation = new Transformation()
                        .Quality("auto")
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            }
            else
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(fileName + extension, stream),
                    Folder = folderName,
                    UseFilename = true,
                    UniqueFilename = true
                };
                //remember to pass cancellation token for long uploads using blobClient "Eman"
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                throw new Exception(uploadResult.Error.Message);
            }

            return new UploadResultDto
            {
                IsSuccess = true,
                PublicId = uploadResult.PublicId,
                Url = uploadResult.SecureUrl?.ToString(),
                ResourceType = isImage ? "image"
                     : isVideo ? "video"
                     : isAudio ? "video"
                     : "raw",
                Size = file.Length
            };
        }

        public async Task<UploadResultDto> UploadRecordingChunkAsync(
            IFormFile file,
            string folderName,
            CancellationToken cancellationToken = default,
            string? publicId = null,
            bool overwrite = false)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty", nameof(file));

            const long maxChunkSize = 100_000_000;
            if (file.Length > maxChunkSize)
                throw new ArgumentException("Recording chunk exceeds the 100 MB size limit.", nameof(file));

            cancellationToken.ThrowIfCancellationRequested();

            var originalFileName = Path.GetFileName(file.FileName);
            var fileName = string.IsNullOrWhiteSpace(originalFileName)
                ? $"chunk-{Guid.NewGuid():N}.webm"
                : originalFileName;

            await using var stream = file.OpenReadStream();
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folderName,
                PublicId = publicId,
                Overwrite = overwrite,
                UseFilename = publicId == null,
                UniqueFilename = publicId == null
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            cancellationToken.ThrowIfCancellationRequested();

            if (uploadResult.Error != null)
            {
                _logger.LogError(
                    "Cloudinary raw chunk upload error for {FileName}: {Error}",
                    fileName,
                    uploadResult.Error.Message);

                return new UploadResultDto
                {
                    IsSuccess = false,
                    OriginalFileName = fileName,
                    ErrorMessage = uploadResult.Error.Message
                };
            }

            return new UploadResultDto
            {
                IsSuccess = true,
                PublicId = uploadResult.PublicId,
                Url = uploadResult.SecureUrl?.ToString(),
                ResourceType = "raw",
                Size = file.Length,
                Format = uploadResult.Format,
                OriginalFileName = fileName
            };
        }
        public async Task<List<UploadResultDto>> UploadFilesAsync(List<IFormFile> files, string folderName = "general", CancellationToken cancellationToken = default)
        {
            if (files == null || !files.Any())
                throw new ArgumentException("No files provided");

            cancellationToken.ThrowIfCancellationRequested();

            var semaphore = new SemaphoreSlim(3); // max 3 parallel uploads

            var uploadTasks = files.Select(async file =>
            {
                await semaphore.WaitAsync(cancellationToken);

                try
                {
                    return await UploadFileAsync(file, folderName, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);

                    return null;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(uploadTasks);

            return results.Where(r => r != null).ToList()!;
        }

        public async Task<bool> DeleteFileAsync(string publicId, CancellationToken cancellationToken = default, ResourceType resourceType = ResourceType.Image)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                throw new ArgumentException("PublicId is required", nameof(publicId));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var deleteParams = new DeletionParams(publicId);
                deleteParams.ResourceType = resourceType;

                var result = await _cloudinary.DestroyAsync(deleteParams);

                cancellationToken.ThrowIfCancellationRequested();

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary delete error: {Error}", result.Error.Message);
                    return false;
                }

                // "ok" → deleted successfully
                // "not found" → already deleted
                if (result.Result == "ok")
                    return true;

                if (result.Result == "not found")
                {
                    _logger.LogWarning("Cloudinary file not found: {PublicId}", publicId);
                    return false;
                }

                _logger.LogWarning("Unexpected Cloudinary delete result: {Result}", result.Result);
                return false;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Delete operation was cancelled for {PublicId}", publicId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from Cloudinary: {PublicId}", publicId);
                return false;
            }
        }

        public async Task<Stream> DownloadFileAsync(string publicId,string resourceType = "raw",CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                throw new ArgumentException("PublicId is required.", nameof(publicId));

            var url = GetFileUrl(publicId, resourceType);

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to download file from Cloudinary. PublicId: {PublicId}, StatusCode: {StatusCode}",
                    publicId, response.StatusCode);

                throw new InvalidOperationException($"Failed to download file from Cloudinary. Status code: {response.StatusCode}");
            }

            var memoryStream = new MemoryStream();
            await using var remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await remoteStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            return memoryStream;
        }

        public async Task<bool> FileExistsAsync(string publicId,string resourceType = "raw",CancellationToken cancellationToken = default)
        {
            var fileInfo = await GetFileInfoAsync(publicId, resourceType, cancellationToken);
            return fileInfo != null;
        }

        public async Task<CloudinaryFileInfoDto?> GetFileInfoAsync(string publicId,string resourceType = "raw",CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                throw new ArgumentException("PublicId is required.", nameof(publicId));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var parsedResourceType = resourceType.ToLowerInvariant() switch
                {
                    "image" => ResourceType.Image,
                    "video" => ResourceType.Video,
                    "raw" => ResourceType.Raw,
                    _ => ResourceType.Raw
                };

                var getResourceParams = new GetResourceParams(publicId)
                {
                    ResourceType = parsedResourceType
                };

                var result = await _cloudinary.GetResourceAsync(getResourceParams);

                if (result == null || result.Error != null)
                {
                    if (result?.Error != null)
                    {
                        _logger.LogWarning(
                            "Cloudinary get resource failed for {PublicId}: {Error}",
                            publicId,
                            result.Error.Message);
                    }

                    return null;
                }

                return new CloudinaryFileInfoDto
                {
                    PublicId = result.PublicId,
                    ResourceType = result.ResourceType.ToString(),
                    Format = result.Format,
                    Bytes = result.Bytes,
                    SecureUrl = result.SecureUrl?.ToString(),
                    CreatedAt = DateTime.TryParse(result.CreatedAt, out var createdAt)
                        ? createdAt
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve Cloudinary resource info for {PublicId}", publicId);
                return null;
            }
        }

        public async Task<UploadResultDto> ReplaceFileAsync(IFormFile file,string publicId,string folderName = "general",CancellationToken cancellationToken = default)
        {
            return await UploadFileAsync(file,folderName,cancellationToken,publicId,overwrite: true);
        }

        public async Task<UploadResultDto> ReplaceStreamAsync(Stream stream,string fileName,string publicId,string folderName = "general",string? contentType = null,CancellationToken cancellationToken = default)
        {
            return await UploadStreamAsync(stream,fileName,folderName,contentType,cancellationToken,publicId,overwrite: true);
        }

        private async Task<UploadResultDto> UploadInternalAsync(Stream stream,string originalFileName,string? contentType,long size,string folderName,CancellationToken cancellationToken,string? publicId,bool overwrite)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extension = Path.GetExtension(originalFileName)?.ToLowerInvariant() ?? string.Empty;

            var baseFileName = !string.IsNullOrWhiteSpace(originalFileName)
                ? Path.GetFileNameWithoutExtension(originalFileName)
                : Guid.NewGuid().ToString();

            var detectedResourceType = DetectResourceType(extension, contentType);

            ValidateFileSize(size, detectedResourceType, extension);

            if (stream.CanSeek)
                stream.Position = 0;

            UploadResult uploadResult;

            if (detectedResourceType == "image")
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(baseFileName + extension, stream),
                    Folder = folderName,
                    PublicId = publicId,
                    Overwrite = overwrite,
                    UseFilename = publicId == null,
                    UniqueFilename = publicId == null,
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto")
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            }
            else if (detectedResourceType == "video")
            {
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(baseFileName + extension, stream),
                    Folder = folderName,
                    PublicId = publicId,
                    Overwrite = overwrite,
                    UseFilename = publicId == null,
                    UniqueFilename = publicId == null,
                    Transformation = new Transformation()
                        .Quality("auto")
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            }
            else
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(baseFileName + extension, stream),
                    Folder = folderName,
                    PublicId = publicId,
                    Overwrite = overwrite,
                    UseFilename = publicId == null,
                    UniqueFilename = publicId == null
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error for {FileName}: {Error}",
                    originalFileName, uploadResult.Error.Message);

                return new UploadResultDto
                {
                    IsSuccess = false,
                    OriginalFileName = originalFileName,
                    ErrorMessage = uploadResult.Error.Message
                };
            }

            return new UploadResultDto
            {
                IsSuccess = true,
                PublicId = uploadResult.PublicId,
                Url = uploadResult.SecureUrl?.ToString(),
                ResourceType = detectedResourceType,
                Size = size,
                Format = uploadResult.Format,
                OriginalFileName = originalFileName
            };
        }

        private static string DetectResourceType(string extension, string? contentType)
        {
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                var normalized = contentType.ToLowerInvariant();

                if (normalized.StartsWith("image/"))
                    return "image";

                if (normalized.StartsWith("video/"))
                    return "video";

                if (normalized.StartsWith("audio/"))
                    return "video"; 
            }

            if (ImageExtensions.Contains(extension))
                return "image";

            if (VideoExtensions.Contains(extension))
                return "video";

            if (AudioExtensions.Contains(extension))
                return "video";

            return "raw";
        }

        private static void ValidateFileSize(long size, string resourceType, string extension)
        {
            if (size <= 0)
                return;

            long maxSize = resourceType switch
            {
                "image" => 10_000_000,
                "video" => 100_000_000,
                "raw" => 100_000_000,
                _ => 10_000_000
            };

            if (size > maxSize)
                throw new Exception($"File exceeds allowed size for {extension}.");
        }
    }
}
