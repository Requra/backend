using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Requra.Infrastructure.ExternalDTOs.CloudinaryDto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalInterfaces.ICloudinaryService
{
    public interface ICloudinaryService
    {
        string GetFileUrl(string publicId, string resourceType = "image");

        Task<UploadResultDto> UploadFileAsync(IFormFile file,string folderName = "general",CancellationToken cancellationToken = default,string? publicId = null,bool overwrite = false);

        Task<UploadResultDto> UploadRecordingChunkAsync(IFormFile file,string folderName,CancellationToken cancellationToken = default,string? publicId = null,bool overwrite = false);

        Task<UploadResultDto> UploadStreamAsync(Stream stream,string fileName,string folderName = "general",string? contentType = null,CancellationToken cancellationToken = default,string? publicId = null,bool overwrite = false);

        Task<List<UploadResultDto>> UploadFilesAsync(List<IFormFile> files,string folderName = "general",CancellationToken cancellationToken = default);

        Task<bool> DeleteFileAsync(string publicId,CancellationToken cancellationToken = default,ResourceType resourceType = ResourceType.Image);

        Task<Stream> DownloadFileAsync(string publicId,string resourceType = "raw",CancellationToken cancellationToken = default);

        Task<bool> FileExistsAsync(string publicId,string resourceType = "raw",CancellationToken cancellationToken = default);

        Task<CloudinaryFileInfoDto?> GetFileInfoAsync(string publicId,string resourceType = "raw",CancellationToken cancellationToken = default);

        Task<UploadResultDto> ReplaceFileAsync(IFormFile file,string publicId,string folderName = "general",CancellationToken cancellationToken = default);

        Task<UploadResultDto> ReplaceStreamAsync(Stream stream,string fileName,string publicId,string folderName = "general",string? contentType = null,CancellationToken cancellationToken = default);


    }
}
