namespace BedrockBoot.Models.Classes.Download;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class ChunkDownloader
{
    private readonly HttpClient _httpClient;
    private readonly int _chunkCount;

    public ChunkDownloader(int chunkCount = 4)
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
        _chunkCount = chunkCount;
    }

    /// <summary>
    /// 分片下载文件
    /// </summary>
    /// <param name="url">文件URL</param>
    /// <param name="outputPath">输出路径</param>
    /// <param name="progress">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DownloadFileAsync(string url, string outputPath,
        IProgress<DownloadProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        // 获取文件总大小
        long fileSize = await GetFileSizeAsync(url, cancellationToken);
        if (fileSize == 0)
            throw new Exception("无法获取文件大小或文件为空");

        // 计算每个分片的大小和范围
        var chunks = CalculateChunks(fileSize, _chunkCount);

        // 创建临时文件目录
        string tempDir = Path.Combine(Path.GetTempPath(), "ChunkDownload", Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 创建进度跟踪器
            var progressTracker = new ProgressTracker(fileSize, chunks.Count, progress);

            // 并行下载所有分片
            var downloadTasks = new List<Task>();
            for (int i = 0; i < chunks.Count; i++)
            {
                int chunkIndex = i;
                var chunk = chunks[i];
                string tempFilePath = Path.Combine(tempDir, $"chunk_{chunkIndex}.tmp");

                downloadTasks.Add(DownloadChunkAsync(
                    url, chunk.Start, chunk.End, tempFilePath,
                    progressTracker, chunkIndex, cancellationToken));
            }

            // 等待所有分片下载完成
            await Task.WhenAll(downloadTasks);

            // 合并所有分片
            await MergeChunksAsync(chunks, tempDir, outputPath, cancellationToken);

            // 报告最终进度
            progress?.Report(new DownloadProgress
            {
                TotalBytes = fileSize,
                DownloadedBytes = fileSize,
                Percentage = 100.0,
                Status = DownloadStatus.Completed,
                ChunksCompleted = chunks.Count,
                TotalChunks = chunks.Count
            });

            Console.WriteLine($"文件下载完成: {outputPath}");
        }
        catch (OperationCanceledException)
        {
            progress?.Report(new DownloadProgress
            {
                TotalBytes = fileSize,
                DownloadedBytes = GetTotalDownloadedBytes(tempDir),
                Percentage = (double)GetTotalDownloadedBytes(tempDir) / fileSize * 100,
                Status = DownloadStatus.Canceled,
                ChunksCompleted = GetCompletedChunksCount(tempDir),
                TotalChunks = chunks.Count
            });
            throw;
        }
        catch (Exception ex)
        {
            progress?.Report(new DownloadProgress
            {
                TotalBytes = fileSize,
                DownloadedBytes = GetTotalDownloadedBytes(tempDir),
                Percentage = (double)GetTotalDownloadedBytes(tempDir) / fileSize * 100,
                Status = DownloadStatus.Failed,
                ErrorMessage = ex.Message,
                ChunksCompleted = GetCompletedChunksCount(tempDir),
                TotalChunks = chunks.Count
            });
            throw;
        }
        finally
        {
            // 清理临时文件
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }

    /// <summary>
    /// 获取文件大小
    /// </summary>
    private async Task<long> GetFileSizeAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        return response.Content.Headers.ContentLength ?? 0;
    }

    /// <summary>
    /// 计算分片范围
    /// </summary>
    private List<ChunkRange> CalculateChunks(long fileSize, int chunkCount)
    {
        var chunks = new List<ChunkRange>();
        long chunkSize = fileSize / chunkCount;

        for (int i = 0; i < chunkCount; i++)
        {
            long start = i * chunkSize;
            long end = (i == chunkCount - 1) ? fileSize - 1 : start + chunkSize - 1;

            chunks.Add(new ChunkRange { Start = start, End = end, Index = i });
        }

        return chunks;
    }

    /// <summary>
    /// 下载单个分片
    /// </summary>
    private async Task DownloadChunkAsync(string url, long start, long end, string tempFilePath,
        ProgressTracker progressTracker, int chunkIndex, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        int bytesRead;
        long chunkDownloaded = 0;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            chunkDownloaded += bytesRead;

            // 更新进度
            progressTracker.ReportChunkProgress(chunkIndex, chunkDownloaded);
        }
    }

    /// <summary>
    /// 合并所有分片
    /// </summary>
    private async Task MergeChunksAsync(List<ChunkRange> chunks, string tempDir, string outputPath, CancellationToken cancellationToken)
    {
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        for (int i = 0; i < chunks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string tempFilePath = Path.Combine(tempDir, $"chunk_{i}.tmp");

            using var chunkStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
            await chunkStream.CopyToAsync(outputStream, cancellationToken);
        }
    }

    /// <summary>
    /// 获取已下载的总字节数
    /// </summary>
    private long GetTotalDownloadedBytes(string tempDir)
    {
        long total = 0;
        if (Directory.Exists(tempDir))
        {
            foreach (var file in Directory.GetFiles(tempDir))
            {
                var fileInfo = new FileInfo(file);
                total += fileInfo.Length;
            }
        }
        return total;
    }

    /// <summary>
    /// 获取已完成的分片数量
    /// </summary>
    private int GetCompletedChunksCount(string tempDir)
    {
        if (Directory.Exists(tempDir))
        {
            return Directory.GetFiles(tempDir).Length;
        }
        return 0;
    }

    /// <summary>
    /// 分片范围结构
    /// </summary>
    private class ChunkRange
    {
        public long Start { get; set; }
        public long End { get; set; }
        public int Index { get; set; }
        public long Length => End - Start + 1;
    }

    /// <summary>
    /// 进度跟踪器
    /// </summary>
    private class ProgressTracker
    {
        private readonly long _totalSize;
        private readonly int _totalChunks;
        private readonly IProgress<DownloadProgress> _progress;
        private readonly long[] _chunkProgress;
        private long _lastReportedBytes;
        private DateTime _lastReportTime;

        public ProgressTracker(long totalSize, int totalChunks, IProgress<DownloadProgress> progress)
        {
            _totalSize = totalSize;
            _totalChunks = totalChunks;
            _progress = progress;
            _chunkProgress = new long[totalChunks];
            _lastReportTime = DateTime.Now;
        }

        public void ReportChunkProgress(int chunkIndex, long bytesDownloaded)
        {
            _chunkProgress[chunkIndex] = bytesDownloaded;

            // 计算总下载量
            long totalDownloaded = 0;
            int completedChunks = 0;
            foreach (var progress in _chunkProgress)
            {
                totalDownloaded += progress;
                if (progress > 0) completedChunks++;
            }

            // 计算下载速度
            var now = DateTime.Now;
            var timeElapsed = (now - _lastReportTime).TotalSeconds;
            double speed = timeElapsed > 0 ? (totalDownloaded - _lastReportedBytes) / timeElapsed / 1024 : 0;

            // 限制报告频率（每秒最多报告4次）
            if (timeElapsed >= 0.25 || totalDownloaded == _totalSize)
            {
                _progress?.Report(new DownloadProgress
                {
                    TotalBytes = _totalSize,
                    DownloadedBytes = totalDownloaded,
                    Percentage = (double)totalDownloaded / _totalSize * 100,
                    SpeedKbps = speed,
                    Status = DownloadStatus.Downloading,
                    ChunksCompleted = completedChunks,
                    TotalChunks = _totalChunks
                });

                _lastReportedBytes = totalDownloaded;
                _lastReportTime = now;
            }
        }
    }
}

/// <summary>
/// 下载进度信息
/// </summary>
public class DownloadProgress
{
    /// <summary>
    /// 文件总大小（字节）
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// 已下载字节数
    /// </summary>
    public long DownloadedBytes { get; set; }

    /// <summary>
    /// 下载百分比
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// 下载速度（KB/s）
    /// </summary>
    public double SpeedKbps { get; set; }

    /// <summary>
    /// 下载状态
    /// </summary>
    public DownloadStatus Status { get; set; }

    /// <summary>
    /// 错误信息（如果状态为Failed）
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// 已完成的分片数量
    /// </summary>
    public int ChunksCompleted { get; set; }

    /// <summary>
    /// 总分片数量
    /// </summary>
    public int TotalChunks { get; set; }
}

/// <summary>
/// 下载状态枚举
/// </summary>
public enum DownloadStatus
{
    Downloading,
    Completed,
    Failed,
    Canceled
}