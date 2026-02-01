using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Download;

public class SingleThreadDownloader : IDisposable
{
    private static readonly TimeSpan ProgressReportInterval = TimeSpan.FromMilliseconds(200);
    private readonly int _bufferSize;
    private readonly HttpClient _httpClient;

    /// <summary>
    ///     初始化单线程下载器
    /// </summary>
    /// <param name="bufferSize">缓冲区大小，默认为 81920 字节（80KB）</param>
    /// <param name="timeoutSeconds">超时时间（秒），默认为 30 秒</param>
    public SingleThreadDownloader(int bufferSize = 81920, int timeoutSeconds = 30)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        // 设置默认请求头
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; SingleThreadDownloader/1.0)");

        _bufferSize = bufferSize;
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    /// <summary>
    ///     下载文件并报告进度
    /// </summary>
    /// <param name="url">文件URL</param>
    /// <param name="filePath">保存路径</param>
    /// <param name="progress">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下载是否成功</returns>
    public async Task<bool> DownloadAsync(string url, string filePath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL不能为空", nameof(url));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("文件路径不能为空", nameof(filePath));

        // 修复URL中的空格
        var encodedUrl = EncodeUrl(url);

        try
        {
            // 获取文件信息
            var fileInfo = await GetFileInfoAsync(encodedUrl, cancellationToken);
            var contentLength = fileInfo.contentLength;

            Console.WriteLine($@"开始下载: {url}");
            Console.WriteLine($@"文件大小: {(contentLength.HasValue ? FormatBytes(contentLength.Value) : "未知")}");
            Console.WriteLine($@"保存到: {filePath}");

            // 创建目录
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            // 执行下载
            return await DownloadFileAsync(encodedUrl, filePath, contentLength, progress, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine(@"下载已取消");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"下载失败: {ex.Message}");
            throw new Exception($"下载失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     获取文件信息
    /// </summary>
    private async Task<(long? contentLength, bool supportsRange)> GetFileInfoAsync(string url,
        CancellationToken cancellationToken)
    {
        try
        {
            // 先尝试HEAD请求
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
            using var headResponse = await _httpClient.SendAsync(headRequest,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (headResponse.IsSuccessStatusCode)
            {
                var contentLength = headResponse.Content.Headers.ContentLength;
                var supportsRange = headResponse.Headers.AcceptRanges?.Contains("bytes") == true;

                return (contentLength, supportsRange);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"HEAD请求失败，尝试GET: {ex.Message}");
        }

        // HEAD失败，尝试GET请求前几个字节
        try
        {
            using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
            getRequest.Headers.Range = new RangeHeaderValue(0, 0);

            using var getResponse = await _httpClient.SendAsync(getRequest,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (getResponse.IsSuccessStatusCode && getResponse.StatusCode == HttpStatusCode.PartialContent)
            {
                var contentLength = getResponse.Content.Headers.ContentLength;
                return (contentLength, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"带Range的GET请求失败: {ex.Message}");
        }

        return (null, false);
    }

    /// <summary>
    ///     执行文件下载
    /// </summary>
    private async Task<bool> DownloadFileAsync(string url, string filePath, long? totalBytes,
        IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        using var response =
            await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream =
            new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, true);

        var buffer = new byte[_bufferSize];
        long totalDownloaded = 0;
        var lastReportTime = DateTime.Now;
        long lastReportBytes = 0;

        int bytesRead;
        while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalDownloaded += bytesRead;

            // 报告进度
            ReportProgress(totalDownloaded, totalBytes, ref lastReportTime, ref lastReportBytes, progress);

            cancellationToken.ThrowIfCancellationRequested();
        }

        // 最终报告
        if (progress != null)
            progress.Report(new DownloadProgress
            {
                TotalBytes = totalBytes ?? totalDownloaded,
                DownloadedBytes = totalDownloaded,
                BytesPerSecond = 0,
                EstimatedRemainingSeconds = 0
            });

        Console.WriteLine($@"下载完成: {FormatBytes(totalDownloaded)}");
        return true;
    }

    /// <summary>
    ///     报告下载进度
    /// </summary>
    private void ReportProgress(long downloaded, long? totalBytes,
        ref DateTime lastReportTime, ref long lastReportBytes,
        IProgress<DownloadProgress>? progress)
    {
        if (progress == null) return;

        var now = DateTime.Now;
        var timeSinceLastReport = (now - lastReportTime).TotalSeconds;

        // 至少200ms报告一次
        if (timeSinceLastReport >= ProgressReportInterval.TotalSeconds)
        {
            var bytesSinceLastReport = downloaded - lastReportBytes;
            var bytesPerSecond = bytesSinceLastReport / timeSinceLastReport;

            var progressInfo = new DownloadProgress
            {
                TotalBytes = totalBytes ?? downloaded,
                DownloadedBytes = downloaded,
                BytesPerSecond = bytesPerSecond
            };

            // 计算剩余时间
            if (totalBytes.HasValue && totalBytes.Value > 0 && bytesPerSecond > 0)
            {
                var remainingBytes = totalBytes.Value - downloaded;
                progressInfo.EstimatedRemainingSeconds = remainingBytes / bytesPerSecond;
            }

            progress.Report(progressInfo);

            lastReportTime = now;
            lastReportBytes = downloaded;
        }
    }

    /// <summary>
    ///     编码URL中的特殊字符
    /// </summary>
    private string EncodeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        // 简单处理：替换空格为%20
        return url.Replace(" ", "%20");
    }

    /// <summary>
    ///     格式化字节数为易读格式
    /// </summary>
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        var order = 0;
        double len = bytes;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    ///     下载进度信息
    /// </summary>
    public class DownloadProgress
    {
        /// <summary>
        ///     总字节数（如果未知则为 -1）
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        ///     已下载字节数
        /// </summary>
        public long DownloadedBytes { get; set; }

        /// <summary>
        ///     下载进度百分比（0-100）
        /// </summary>
        public double ProgressPercentage => TotalBytes > 0
            ? Math.Min(100, Math.Round((double)DownloadedBytes / TotalBytes * 100, 2))
            : 0;

        /// <summary>
        ///     下载速度（字节/秒）
        /// </summary>
        public double BytesPerSecond { get; set; }

        /// <summary>
        ///     预估剩余时间（秒）
        /// </summary>
        public double EstimatedRemainingSeconds { get; set; }
    }
}