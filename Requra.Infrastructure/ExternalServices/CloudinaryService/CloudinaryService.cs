using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Requra.Infrastructure.ExternalDTOs.CloudinaryDto;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalServices.CloudinaryService
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
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


        public string GetFileUrl(string publicId)
        {
            return _cloudinary.Api.UrlImgUp.BuildUrl(publicId);
        }

        public async Task<UploadResultDto> UploadFileAsync(IFormFile file,string folderName = "general",CancellationToken cancellationToken = default)
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
            var audioExtensions = new[] {".mp3", ".wav", ".aac", ".flac", ".ogg", ".m4a"};

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
                    UseFilename = true,
                    UniqueFilename = true,

                    //Optimization
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto")
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            }
            else if (isVideo|isAudio)
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
                PublicId = uploadResult.PublicId,
                Url = uploadResult.SecureUrl.ToString(),
                ResourceType = isImage ? "image"
                     : isVideo ? "video"
                     : isAudio ? "video"
                     : "raw",
                Size = file.Length
            };
        }
        public async Task<List<UploadResultDto>> UploadFilesAsync(List<IFormFile> files,string folderName = "general",CancellationToken cancellationToken = default)
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

        public async Task<bool> DeleteFileAsync(string publicId,CancellationToken cancellationToken = default,ResourceType resourceType=ResourceType.Image)
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

    


    }
    }
