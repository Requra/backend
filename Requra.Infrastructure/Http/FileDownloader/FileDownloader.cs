using Requra.Application.Interfaces.IFileDownloader;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Http.FileDownloader
{
    public class FileDownloader : IFileDownloader
    {
        private readonly HttpClient _httpClient;

        public FileDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<byte[]> DownloadAsync(string url)
        {
            return await _httpClient.GetByteArrayAsync(url);
        }
    }
}
