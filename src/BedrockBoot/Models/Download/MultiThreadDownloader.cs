using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry;

namespace BedrockBoot.Models.Download;

public class MultiThreadDownloader : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SocketsHttpHandler _handler;
    private readonly int _maxConcurrency;
    private readonly int _bufferSize;
    private readonly TimeSpan _defaultTimeout;
    private static readonly TimeSpan ProgressReportInterval = TimeSpan.FromMilliseconds(1000);
    private const long MinPartSize = 5 * 1024 * 1024; // 最小5MB的分段大小
    private const long MaxPartSize = 50 * 1024 * 1024; // 最大50MB的分段大小

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="maxConcurrency">最大并发下载线程数</param>
    /// <param name="bufferSize">每次读写操作的缓冲区大小</param>
    /// <param name="defaultTimeoutSeconds">默认的单个 HTTP 请求超时时间（秒）默认为 20 秒</param>
    public MultiThreadDownloader(int maxConcurrency = 4, int bufferSize = 81920, int defaultTimeoutSeconds = 20)
    {
        _handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            MaxConnectionsPerServer = maxConcurrency * 2,
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            UseProxy = false,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };

        _httpClient = new HttpClient(_handler);
        _defaultTimeout = TimeSpan.FromSeconds(defaultTimeoutSeconds);
        _httpClient.Timeout = _defaultTimeout;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ImprovedMultiThreadDownloader/1.0)");
        _maxConcurrency = maxConcurrency;
        _bufferSize = bufferSize;
    }
    private async System.Threading.Tasks.Task<(long fileSize, bool supportsRange)> GetFileInfoAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await _httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (headResponse.IsSuccessStatusCode)
            {
                var contentLength = headResponse.Content.Headers.ContentLength ?? -1;
                var supportsRange = headResponse.Headers.AcceptRanges?.ToString().Equals("bytes", StringComparison.OrdinalIgnoreCase) == true;
                return (contentLength, supportsRange);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HEAD 请求失败: {ex.Message}");
        }

        try
        {
            using var getRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            getRequest.Headers.Range = new RangeHeaderValue(0, 1);
            using var getResponse = await _httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (getResponse.IsSuccessStatusCode)
            {
                var contentRange = getResponse.Content.Headers.ContentRange;
                var supportsRange = contentRange != null && contentRange.HasLength;
                var contentLength = contentRange?.Length ?? getResponse.Content.Headers.ContentLength ?? -1;
                return (contentLength, supportsRange);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"带 Range 的 GET 请求失败: {ex.Message}");
        }

        try
        {
            using var fullRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            using var fullResponse = await _httpClient.SendAsync(fullRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (fullResponse.IsSuccessStatusCode)
            {
                var contentLength = fullResponse.Content.Headers.ContentLength ?? -1;
                return (contentLength, false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"完整 GET 请求失败: {ex.Message}");
        }

        return (-1, false);
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="url">要下载的文件的 URL</param>
    /// <param name="filePath">保存文件的本地路径</param>
    /// <param name="progress">用于报告下载进度的回调</param>
    /// <param name="cancellationToken">用于取消操作的令牌</param>
    /// <returns>如果下载成功则返回 true，否则返回 false</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HttpClient))]
    public async System.Threading.Tasks.Task<bool> DownloadAsync(string url, string filePath, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new Exception("错误: URL 不能为空或空白");
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new Exception("错误: 文件路径不能为空或空白");
        }

        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch (UriFormatException ex)
        {
            throw new Exception($"错误: URL 格式无效{ex.Message}");
        }

        try
        {
            var (fileSize, supportsRange) = await GetFileInfoAsync(uri, cancellationToken);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (fileSize > 0 && supportsRange)
            {
                Console.WriteLine($"服务器支持断点续传且文件大小已知 ({fileSize} bytes)，使用多线程下载...");
                await DownloadMultiPartAsync(uri, filePath, fileSize, progress, cancellationToken);
            }
            else if (fileSize > 0 && !supportsRange)
            {
                Console.WriteLine($"文件大小已知 ({fileSize} bytes) 但服务器不支持断点续传，使用单线程下载...");
                await DownloadSinglePartAsync(uri, filePath, fileSize, progress, cancellationToken);
            }
            else
            {
                Console.WriteLine("无法获取文件大小或服务器不支持必要的功能，使用流式单线程下载 (无法显示进度百分比)...");
                await DownloadAsStreamAsync(uri, filePath, progress, cancellationToken);
            }

            Console.WriteLine($"文件已成功下载并保存到: {filePath}");
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("下载已被取消");
            return false;
        }
        catch (System.Threading.Tasks.TaskCanceledException ex) when (ex.InnerException is TimeoutException || (ex.InnerException == null && ex.CancellationToken == default))
        {
            throw new Exception($"下载失败: 请求超时 (超过 {_defaultTimeout.TotalSeconds} 秒)");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"下载失败: 网络请求错误{ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"下载过程中发生未预期的错误: {ex}");
        }
    }

    private async System.Threading.Tasks.Task DownloadMultiPartAsync(Uri uri, string filePath, long fileSize, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        var actualParts = _maxConcurrency;
        Console.WriteLine($"使用 {actualParts} 个分段进行下载 (文件大小: {fileSize} bytes)");

        // 创建临时文件
        var tempDir = Path.GetTempPath();
        var guid = Guid.NewGuid().ToString("N");
        var tempFilePrefix = Path.Combine(tempDir, $"dl_{guid}_part");

        // 创建分段信息
        var parts = CalculateParts(fileSize, actualParts);
        var tempFiles = new string[parts.Count];
        var tasks = new List<System.Threading.Tasks.Task>();
        var downloadInfos = new PartDownloadInfo[parts.Count];

        // 使用线程安全的进度管理器
        var progressManager = new ProgressManager(fileSize, progress);

        // 创建监控任务
        var monitorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var monitorTask = MonitorDownloadProgress(downloadInfos, monitorCts.Token);

        try
        {
            // 启动所有分段下载任务
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                var tempFilePath = $"{tempFilePrefix}{i}.tmp";
                tempFiles[i] = tempFilePath;

                var downloadInfo = new PartDownloadInfo
                {
                    PartIndex = i,
                    Start = part.Start,
                    End = part.End,
                    Downloaded = 0,
                    LastActivity = DateTime.UtcNow
                };
                downloadInfos[i] = downloadInfo;

                // 创建分段下载任务
                tasks.Add(System.Threading.Tasks.Task.Run(async () =>
                {
                    await DownloadPartWithRetryAsync(
                        uri, part.Start, part.End, tempFilePath,
                        i, downloadInfo, progressManager, cancellationToken);
                }, cancellationToken));
            }

            // 启动进度报告任务
            var progressTask = progressManager.StartReportingAsync(cancellationToken);

            // 等待所有下载任务完成
            await System.Threading.Tasks.Task.WhenAll(tasks);

            // 停止进度报告和监控
            progressManager.StopReporting();
            await progressTask;

            monitorCts.Cancel();
            try { await monitorTask; } catch { /* 忽略取消异常 */ }

            // 合并临时文件
            Console.WriteLine("开始合并临时文件...");
            await MergeTempFilesAsync(tempFiles, filePath, cancellationToken);

            // 报告最终进度
            progressManager.ReportFinalProgress();

            Console.WriteLine("文件合并完成");
        }
        catch (Exception)
        {
            // 停止进度报告
            progressManager?.StopReporting();
            monitorCts.Cancel();
            CleanupTempFiles(tempFiles);
            throw;
        }
        finally
        {
            CleanupTempFiles(tempFiles);
        }
    }

    private async System.Threading.Tasks.Task DownloadPartWithRetryAsync(Uri uri, long start, long end,
        string tempFilePath, int partIndex, PartDownloadInfo downloadInfo, 
        ProgressManager progressManager, CancellationToken cancellationToken,
        int maxRetries = 3)
    {
        for (int retry = 0; retry <= maxRetries; retry++)
        {
            try
            {
                await DownloadPartAsync(uri, start, end, tempFilePath, partIndex,
                    downloadInfo, progressManager, cancellationToken);
                return;
            }
            catch (Exception ex) when (retry < maxRetries)
            {
                Console.WriteLine($"分段 {partIndex} 下载失败，第 {retry + 1} 次重试: {ex.Message}");
                
                // 从进度管理中减去已下载的部分
                progressManager.SubtractDownloaded(downloadInfo.Downloaded);
                
                // 重置下载信息
                downloadInfo.Downloaded = 0;
                downloadInfo.LastActivity = DateTime.UtcNow;
                
                // 等待一段时间后重试
                await System.Threading.Tasks.Task.Delay(1000 * (int)Math.Pow(2, retry), cancellationToken);
                
                // 删除可能损坏的临时文件
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
            }
        }

        throw new Exception($"分段 {partIndex} 下载失败，已达到最大重试次数");
    }

    private async System.Threading.Tasks.Task DownloadPartAsync(Uri uri, long start, long end,
        string tempFilePath, int partIndex, PartDownloadInfo downloadInfo, 
        ProgressManager progressManager, CancellationToken cancellationToken)
    {
        using var partHttpClient = CreatePartHttpClient();
        
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Range = new RangeHeaderValue(start, end);
        
        using var response = await partHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, FileOptions.Asynchronous);

        var buffer = new byte[_bufferSize];
        int bytesRead;
        long partDownloaded = 0;

        var stopwatch = Stopwatch.StartNew();
        long lastSpeedCheckBytes = 0;
        var lastSpeedCheckTime = DateTime.UtcNow;

        while ((bytesRead = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            
            // 更新分段下载信息
            partDownloaded += bytesRead;
            downloadInfo.Downloaded = partDownloaded;
            downloadInfo.LastActivity = DateTime.UtcNow;
            
            // 更新总进度
            progressManager.AddDownloaded(bytesRead);
            
            // 检查下载速度
            var now = DateTime.UtcNow;
            if ((now - lastSpeedCheckTime).TotalSeconds >= 5)
            {
                var speed = (partDownloaded - lastSpeedCheckBytes) / (now - lastSpeedCheckTime).TotalSeconds;
                lastSpeedCheckBytes = partDownloaded;
                lastSpeedCheckTime = now;
                
                if (speed < 1024 && partDownloaded < (end - start + 1) * 0.9)
                {
                    Console.WriteLine($"分段 {partIndex} 下载速度较慢: {speed:F2} B/s");
                }
            }
        }

        Console.WriteLine($"分段 {partIndex} 下载完成: {partDownloaded} bytes, 耗时: {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    private async System.Threading.Tasks.Task MonitorDownloadProgress(PartDownloadInfo[] downloadInfos, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(10000, cancellationToken); // 每10秒检查一次
                
                var now = DateTime.UtcNow;
                var stalledParts = downloadInfos
                    .Where(info => info != null)
                    .Where(info => (now - info.LastActivity).TotalSeconds > 30)
                    .Where(info => info.Downloaded < (info.End - info.Start + 1))
                    .ToList();
                
                if (stalledParts.Any())
                {
                    Console.WriteLine($"警告: 检测到 {stalledParts.Count} 个分段下载停滞:");
                    foreach (var info in stalledParts)
                    {
                        Console.WriteLine($"  分段 {info.PartIndex}: 已下载 {info.Downloaded}/{info.End - info.Start + 1} bytes, 最后活动: {info.LastActivity:HH:mm:ss}");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"监控任务出错: {ex.Message}");
            }
        }
    }

    // 进度管理器类 - 新增
    private class ProgressManager
    {
        private readonly long _totalBytes;
        private readonly IProgress<DownloadProgress>? _progress;
        private long _downloadedBytes;
        private DateTimeOffset _lastReportTime;
        private readonly object _lock = new object();
        private volatile bool _isReporting = true;
        private Timer? _reportTimer;

        public ProgressManager(long totalBytes, IProgress<DownloadProgress>? progress)
        {
            _totalBytes = totalBytes;
            _progress = progress;
            _downloadedBytes = 0;
            _lastReportTime = DateTimeOffset.UtcNow;
        }

        public void AddDownloaded(long bytes)
        {
            Interlocked.Add(ref _downloadedBytes, bytes);
        }

        public void SubtractDownloaded(long bytes)
        {
            Interlocked.Add(ref _downloadedBytes, -bytes);
        }

        public async System.Threading.Tasks.Task StartReportingAsync(CancellationToken cancellationToken)
        {
            if (_progress == null) return;

            while (_isReporting && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await System.Threading.Tasks.Task.Delay(ProgressReportInterval, cancellationToken);
                    ReportProgress(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public void StopReporting()
        {
            _isReporting = false;
        }

        public void ReportProgress(bool force = false)
        {
            if (_progress == null) return;

            var now = DateTimeOffset.UtcNow;
            var shouldReport = force || (now - _lastReportTime) >= ProgressReportInterval;

            if (shouldReport)
            {
                lock (_lock)
                {
                    if (force || (now - _lastReportTime) >= ProgressReportInterval)
                    {
                        _lastReportTime = now;
                        var downloaded = Interlocked.Read(ref _downloadedBytes);
                        _progress.Report(new DownloadProgress
                        {
                            TotalBytes = _totalBytes,
                            DownloadedBytes = downloaded
                        });
                        
                        // 可选：输出调试信息
                        if (_totalBytes > 0)
                        {
                            var percentage = (double)downloaded / _totalBytes * 100;
                            Console.WriteLine($"进度: {downloaded}/{_totalBytes} bytes ({percentage:F2}%)");
                        }
                    }
                }
            }
        }

        public void ReportFinalProgress()
        {
            if (_progress == null) return;
            
            var downloaded = Interlocked.Read(ref _downloadedBytes);
            _progress.Report(new DownloadProgress
            {
                TotalBytes = _totalBytes,
                DownloadedBytes = _totalBytes > 0 ? _totalBytes : downloaded
            });
        }
    }

    private int CalculateOptimalParts(long fileSize)
    {
        if (fileSize <= MinPartSize)
            return 1;

        // 计算理想的分段数量
        var idealParts = (int)Math.Min(
            _maxConcurrency,
            Math.Ceiling((double)fileSize / MinPartSize)
        );

        // 确保分段大小不超过最大值
        var partSize = fileSize / idealParts;
        if (partSize > MaxPartSize)
        {
            idealParts = (int)Math.Ceiling((double)fileSize / MaxPartSize);
        }

        return Math.Max(1, idealParts);
    }

    private List<(long Start, long End)> CalculateParts(long fileSize, int parts)
    {
        var result = new List<(long Start, long End)>();
        var partSize = fileSize / parts;
        var remainder = fileSize % parts;

        long currentStart = 0;
        for (int i = 0; i < parts; i++)
        {
            var currentPartSize = partSize + (i < remainder ? 1 : 0);
            var currentEnd = currentStart + currentPartSize - 1;

            // 确保不超过文件大小
            if (currentEnd >= fileSize)
                currentEnd = fileSize - 1;

            if (i == parts - 1) // 最后一个分段
                currentEnd = fileSize - 1;

            result.Add((currentStart, currentEnd));
            currentStart = currentEnd + 1;
        }

        // 打印分段信息以便调试
        for (int i = 0; i < result.Count; i++)
        {
            var (start, end) = result[i];
            Console.WriteLine($"分段 {i}: {start}-{end}, 大小: {end - start + 1} bytes");
        }

        return result;
    }

    private async System.Threading.Tasks.Task DownloadPartWithRetryAsync(Uri uri, long start, long end,
        string tempFilePath, int partIndex, PartDownloadInfo downloadInfo, long totalDownloadedBytes, Action<bool> reportProgress, CancellationToken cancellationToken,
        int maxRetries = 3)
    {
        for (int retry = 0; retry <= maxRetries; retry++)
        {
            try
            {
                await DownloadPartAsync(uri, start, end, tempFilePath, partIndex,
                    downloadInfo, totalDownloadedBytes, reportProgress, cancellationToken);
                return;
            }
            catch (Exception ex) when (retry < maxRetries)
            {
                Console.WriteLine($"分段 {partIndex} 下载失败，第 {retry + 1} 次重试: {ex.Message}");
                
                // 重置下载信息
                downloadInfo.Downloaded = 0;
                downloadInfo.LastActivity = DateTime.UtcNow;
                
                // 等待一段时间后重试（指数退避）
                await System.Threading.Tasks.Task.Delay(1000 * (int)Math.Pow(2, retry), cancellationToken);
                
                // 删除可能损坏的临时文件
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
            }
        }

        throw new Exception($"分段 {partIndex} 下载失败，已达到最大重试次数");
    }

    private async System.Threading.Tasks.Task DownloadPartAsync(Uri uri, long start, long end,
        string tempFilePath, int partIndex, PartDownloadInfo downloadInfo, long totalDownloadedBytes, Action<bool> reportProgress, CancellationToken cancellationToken)
    {
        // 为每个分段创建独立的HttpClient，避免连接池竞争
        using var partHttpClient = CreatePartHttpClient();
        
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Range = new RangeHeaderValue(start, end);
        
        using var response = await partHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, FileOptions.Asynchronous);

        var buffer = new byte[_bufferSize];
        int bytesRead;
        long partDownloaded = 0;

        var stopwatch = Stopwatch.StartNew();
        long lastSpeedCheckBytes = 0;
        var lastSpeedCheckTime = DateTime.UtcNow;

        while ((bytesRead = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            
            // 更新统计信息
            partDownloaded += bytesRead;
            downloadInfo.Downloaded = partDownloaded;
            downloadInfo.LastActivity = DateTime.UtcNow;
            
            var newTotal = Interlocked.Add(ref totalDownloadedBytes, bytesRead);
            
            // 检查下载速度
            var now = DateTime.UtcNow;
            if ((now - lastSpeedCheckTime).TotalSeconds >= 5)
            {
                var speed = (partDownloaded - lastSpeedCheckBytes) / (now - lastSpeedCheckTime).TotalSeconds;
                lastSpeedCheckBytes = partDownloaded;
                lastSpeedCheckTime = now;
                
                if (speed < 1024 && partDownloaded < (end - start + 1) * 0.9)
                {
                    Console.WriteLine($"分段 {partIndex} 下载速度较慢: {speed:F2} B/s");
                }
            }
            
            // 报告进度
            reportProgress(false);
        }

        Console.WriteLine($"分段 {partIndex} 下载完成: {partDownloaded} bytes, 耗时: {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    private HttpClient CreatePartHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromSeconds(30), // 短连接生命周期
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(15),
            MaxConnectionsPerServer = 1, // 每个分段使用独立连接
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            UseProxy = false
        };
        
        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(60) // 分段单独的超时设置
        };
    }

    private async System.Threading.Tasks.Task DownloadSinglePartAsync(Uri uri, string filePath, long fileSize, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, useAsync: true);

        var buffer = new byte[_bufferSize];
        int bytesRead;
        long totalBytesRead = 0;
        var lastReportTime = DateTimeOffset.UtcNow;

        void ReportProgressIfNeeded()
        {
            bool shouldReport = (DateTimeOffset.UtcNow - lastReportTime) >= ProgressReportInterval;
            if (shouldReport)
            {
                lastReportTime = DateTimeOffset.UtcNow;
                progress?.Report(new DownloadProgress
                {
                    TotalBytes = fileSize,
                    DownloadedBytes = totalBytesRead
                });
            }
        }

        while ((bytesRead = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;
            ReportProgressIfNeeded();
        }

        progress?.Report(new DownloadProgress
        {
            TotalBytes = fileSize,
            DownloadedBytes = totalBytesRead
        });
    }

    private async System.Threading.Tasks.Task DownloadAsStreamAsync(Uri uri, string filePath, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, useAsync: true);

        var buffer = new byte[_bufferSize];
        int bytesRead;
        long totalBytesRead = 0;
        var lastReportTime = DateTimeOffset.UtcNow;

        void ReportProgressIfNeeded()
        {
            bool shouldReport = (DateTimeOffset.UtcNow - lastReportTime) >= ProgressReportInterval;
            if (shouldReport)
            {
                lastReportTime = DateTimeOffset.UtcNow;
                progress?.Report(new DownloadProgress
                {
                    TotalBytes = -1,
                    DownloadedBytes = totalBytesRead
                });
            }
        }

        while ((bytesRead = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;
            ReportProgressIfNeeded();
        }

        progress?.Report(new DownloadProgress
        {
            TotalBytes = -1,
            DownloadedBytes = totalBytesRead
        });
    }

    private async System.Threading.Tasks.Task MergeTempFilesAsync(string[] tempFiles, string outputPath, CancellationToken cancellationToken)
    {
        Thread.Sleep(1000);
        // 增加缓冲区大小
        const int largeBufferSize = 81920 * 4; // 320KB缓冲区

        using var outputStream = new BufferedStream(
            new FileStream(outputPath, FileMode.Create, FileAccess.Write,
                FileShare.None, largeBufferSize, FileOptions.WriteThrough | FileOptions.Asynchronous),
            largeBufferSize * 2);

        // 按顺序处理文件
        var sortedFiles = tempFiles
            .Where(File.Exists)
            .Select(f => new FileInfo(f))
            .ToArray();

        foreach (var fileInfo in sortedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
        
            Console.WriteLine($"正在合并文件: {fileInfo.FullName}, 大小: {fileInfo.Length} bytes");
        
            using var inputStream = new BufferedStream(
                new FileStream(fileInfo.FullName, FileMode.Open,
                    FileAccess.Read, FileShare.Read, largeBufferSize,
                    FileOptions.SequentialScan | FileOptions.Asynchronous),
                largeBufferSize * 2);
        
            var buffer = new byte[largeBufferSize];
            int bytesRead;
        
            // 手动复制以支持进度报告
            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await outputStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            }
        }
    
        await outputStream.FlushAsync(cancellationToken);
    }

    private void CleanupTempFiles(string[] tempFiles)
    {
        foreach (var tempFile in tempFiles)
        {
            if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除临时文件失败 {tempFile}: {ex.Message}");
                }
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _handler?.Dispose();
    }


    private class PartDownloadInfo
    {
        public int PartIndex { get; set; }
        public long Start { get; set; }
        public long End { get; set; }
        public long Downloaded { get; set; }
        public DateTime LastActivity { get; set; }
    }
}