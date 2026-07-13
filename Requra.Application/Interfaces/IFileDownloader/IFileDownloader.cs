using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IFileDownloader
{
    public interface IFileDownloader
    {
        Task<byte[]> DownloadAsync(string url);
    }
}
