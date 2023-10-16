using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ToolsUtilities;

namespace ToolsUtilitiesStandard.Network
{

    public class NetworkManager
    {
        static NetworkManager mSelf;
        public static NetworkManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new NetworkManager();
                }
                return mSelf;
            }
        }

        public async Task<ToolsUtilities.GeneralResponse> DownloadWithProgress(HttpClient _httpClient, string url,
            FilePath destination, Action<long?, long> progressChanged, CancellationToken cancellationToken = default)
        {
            var generalResponse = ToolsUtilities.GeneralResponse.SuccessfulResponse;


            try
            {
                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    var totalBytes = response.Content.Headers.ContentLength;
                    progressChanged?.Invoke(totalBytes, 0);

                    if(destination.Exists())
                    {
                        System.IO.File.Delete(destination.FullPath);
                    }

                    if(!Directory.Exists(destination.GetDirectoryContainingThis().FullPath))
                    {
                        System.IO.Directory.CreateDirectory(destination.GetDirectoryContainingThis().FullPath);
                    }

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var destinationStream = System.IO.File.Open(destination.FullPath, FileMode.Create))
                    {
                        await ProcessContentStream(totalBytes, contentStream, destinationStream, progressChanged, cancellationToken);
                    }
                }
            }
            catch (Exception e)
            {
                generalResponse = ToolsUtilities.GeneralResponse.UnsuccessfulWith(e.Message);
            }
            return generalResponse;

        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream, 
            Stream destinationStream, Action<long?, long> progressChanged, CancellationToken cancellationToken = default)
        {
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    isMoreToRead = false;
                    progressChanged?.Invoke(totalDownloadSize, totalBytesRead);
                    continue;
                }

                await destinationStream.WriteAsync(buffer, 0, bytesRead);

                totalBytesRead += bytesRead;
                readCount += 1;

                progressChanged?.Invoke(totalDownloadSize, totalBytesRead);

            }
            while (isMoreToRead);
        }

        public static string ToMemoryDisplay(long? bytes)
        {
            if (bytes == null)
            {
                return "Unknown";
            }
            else if (bytes < 1024)
            {
                return $"{bytes} B";
            }
            else if (bytes < 1024 * 1024)
            {
                return $"{bytes / 1024} KB";
            }
            //else if(bytes < 1024L * 1024L * 1024L)
            {
                var mb = bytes.Value / (1024 * 1024);

                var extraKb = bytes.Value - (mb * 1024 * 1024);

                var hundredKb = extraKb / (100 * 1000);
                hundredKb = Math.Min(9, hundredKb);
                return $"{bytes / (1024 * 1024)}.{hundredKb:0} MB";
            }
        }
    }
}
