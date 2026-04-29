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
        string GetFileUrl(string publicId);

        Task<UploadResultDto> UploadFileAsync(IFormFile file, string folderName = "general", CancellationToken cancellationToken = default,string ? publicId = null,bool overwrite = false);

        Task<List<UploadResultDto>> UploadFilesAsync(List<IFormFile> files, string folderName = "general", CancellationToken cancellationToken = default);
        Task<bool> DeleteFileAsync(string publicId, CancellationToken cancellationToken = default, ResourceType resourceType = ResourceType.Image);
    }
}
