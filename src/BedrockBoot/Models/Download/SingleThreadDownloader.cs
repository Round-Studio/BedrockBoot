using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using BedrockBoot.Base.Entry;

namespace ImprovedDownloadManager
{
    public class SingleThreadDownloader : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly int _maxThreads;
        private readonly int _chunkSize;
        private static readonly TimeSpan ProgressReportInterval = TimeSpan.FromMilliseconds(500);

        public class DownloadConfig
        {
            public int MaxThreads { get; set; } = 4;
            public int ChunkSize { get; set; } = 1024 * 1024; // 1MB chunks
            public int MaxRetries { get; set; } = 3;
            public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
            public string UserAgent { get; set; } = "Mozilla/5.0 (compatible; DownloadManager/1.0)";
        }

        public SingleThreadDownloader(DownloadConfig config = null)
        {
            config = config ?? new DownloadConfig();
            
            var handler = new HttpClientHandler()
            {
                MaxConnectionsPerServer = config.MaxThreads,
                UseProxy = false,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = config.Timeout
            };
            
            _httpClient.DefaultRequestHeaders.Add("User-Agent", config.UserAgent);
            _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            
            _maxThreads = config.MaxThreads;
            _chunkSize = config.ChunkSize;
        }

        public async Task<bool> DownloadAsync(string url, string filePath, 
            IProgress<DownloadProgress> progress = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            // Validate URL format
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) || 
                !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Invalid URL format", nameof(url));
            }

            try
            {
                // Get file info
                var fileInfo = await GetFileInfoAsync(url, cancellationToken);
                
                // Create directory if not exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Download in chunks
                await DownloadInChunksAsync(url, filePath, fileInfo, progress, cancellationToken);
                
                Console.WriteLine($"Download completed: {filePath}");
                return true;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Download was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download failed: {ex.Message}");
                throw;
            }
        }

        private async Task<FileInfoResult> GetFileInfoAsync(string url, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _httpClient.SendAsync(request, 
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            var acceptRanges = response.Headers.AcceptRanges?.Contains("bytes") ?? false;
            
            return new FileInfoResult
            {
                ContentLength = contentLength,
                AcceptRanges = acceptRanges
            };
        }

        private async Task DownloadInChunksAsync(string url, string filePath, FileInfoResult fileInfo, 
            IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
        {
            if (!fileInfo.AcceptRanges || !fileInfo.ContentLength.HasValue || fileInfo.ContentLength <= _chunkSize)
            {
                // Fallback to single thread download
                await DownloadSingleThreadAsync(url, filePath, fileInfo, progress, cancellationToken);
                return;
            }

            var totalSize = fileInfo.ContentLength.Value;
            var chunkCount = (int)Math.Ceiling((double)totalSize / _chunkSize);
            var tasks = new List<Task>();
            var temporaryFiles = new List<string>();
            
            // Create temporary files for each chunk
            for (int i = 0; i < chunkCount; i++)
            {
                var tempFile = Path.GetTempFileName();
                temporaryFiles.Add(tempFile);
                
                var startByte = i * _chunkSize;
                var endByte = Math.Min(startByte + _chunkSize - 1, totalSize - 1);
                
                tasks.Add(DownloadChunkAsync(url, tempFile, startByte, endByte, cancellationToken));
            }

            // Limit concurrent downloads
            var semaphore = new SemaphoreSlim(_maxThreads);
            var limitedTasks = tasks.Select(async task =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    await task;
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();
            
            await Task.WhenAll(limitedTasks);

            // Combine chunks
            await CombineChunksAsync(temporaryFiles, filePath, cancellationToken);

            // Cleanup temporary files
            foreach (var tempFile in temporaryFiles)
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            // Report final progress
            progress?.Report(new DownloadProgress
            {
                TotalBytes = totalSize,
                DownloadedBytes = totalSize,
                BytesPerSecond = 0,
                EstimatedRemainingSeconds = 0
            });
        }

        private async Task DownloadChunkAsync(string url, string filePath, long startByte, long endByte, 
            CancellationToken cancellationToken, int retryCount = 0)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startByte, endByte);
                
                using var response = await _httpClient.SendAsync(request, 
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                
                response.EnsureSuccessStatusCode();

                using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, 
                    FileShare.None, _chunkSize, FileOptions.Asynchronous);
                
                await responseStream.CopyToAsync(fileStream, cancellationToken);
            }
            catch (HttpRequestException) when (retryCount < 3)
            {
                await Task.Delay(1000 * (retryCount + 1), cancellationToken); // Exponential backoff
                await DownloadChunkAsync(url, filePath, startByte, endByte, cancellationToken, retryCount + 1);
            }
        }

        private async Task CombineChunksAsync(List<string> chunkFiles, string outputFile, CancellationToken cancellationToken)
        {
            using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, 
                FileShare.None, _chunkSize, FileOptions.Asynchronous);
            
            foreach (var chunkFile in chunkFiles)
            {
                using var inputStream = new FileStream(chunkFile, FileMode.Open, FileAccess.Read, 
                    FileShare.Read, _chunkSize, FileOptions.Asynchronous);
                
                await inputStream.CopyToAsync(outputStream, cancellationToken);
            }
        }

        private async Task DownloadSingleThreadAsync(string url, string filePath, FileInfoResult fileInfo, 
            IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, 
                FileShare.None, _chunkSize, FileOptions.Asynchronous);

            var buffer = new byte[_chunkSize];
            long totalDownloaded = 0;
            var totalSize = fileInfo.ContentLength ?? -1;
            var lastReportTime = DateTime.UtcNow;
            
            int bytesRead;
            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalDownloaded += bytesRead;
                
                var now = DateTime.UtcNow;
                if (now - lastReportTime >= ProgressReportInterval)
                {
                    progress?.Report(new DownloadProgress
                    {
                        TotalBytes = totalSize,
                        DownloadedBytes = totalDownloaded,
                        BytesPerSecond = 0, // Could implement speed calculation here
                        EstimatedRemainingSeconds = 0
                    });
                    lastReportTime = now;
                }
                
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    internal class FileInfoResult
    {
        public long? ContentLength { get; set; }
        public bool AcceptRanges { get; set; }
    }
}