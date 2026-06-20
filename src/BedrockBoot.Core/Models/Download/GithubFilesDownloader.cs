using System.Collections.Concurrent;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Global;

namespace BedrockBoot.Core.Models.Download;

public class GithubFilesDownloader
{
    private readonly MultiThreadDownloader _downloader;

    public GithubFilesDownloader(int maxConcurrency = 4, int bufferSize = 81920, int defaultTimeoutSeconds = 20)
    {
        _downloader = new MultiThreadDownloader(maxConcurrency, bufferSize, defaultTimeoutSeconds);
    }

    /// <summary>
    /// 测试下载源速度 - 第一个成功完成的源即被使用
    /// </summary>
    /// <param name="fileUrl">原始文件URL</param>
    /// <param name="testSize">测试下载的大小（字节）</param>
    /// <param name="timeoutSeconds">每个源的超时时间</param>
    /// <returns>第一个成功完成的下载源</returns>
    private async Task<(string SourceName, string Url)> TestDownloadSourcesAsync(
        string fileUrl, 
        long testSize = 1024 * 512, // 默认测试0.5MB
        int timeoutSeconds = 20) // 减少超时时间以更快响应
    {
        var cts = new CancellationTokenSource();
        var completionSource = new TaskCompletionSource<(string SourceName, string Url)>();
        var testTasks = new List<Task>();
        
        // 记录已经开始测试但未完成的源，用于后续取消
        var activeSources = new ConcurrentDictionary<string, CancellationTokenSource>();

        foreach (var source in SourceList.UpdateDownloadSources)
        {
            var sourceKey = source.Key;
            var sourcePattern = source.Value;
            var sourceCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            
            activeSources[sourceKey] = sourceCts;

            testTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await TestSingleSourceAsync(
                        sourceKey, 
                        sourcePattern, 
                        fileUrl, 
                        testSize, 
                        timeoutSeconds,
                        sourceCts.Token);

                    if (result.Speed > 0 && !completionSource.Task.IsCompleted)
                    {
                        // 成功完成测试，设置结果并取消其他测试
                        var selectedUrl = SourceList.UpdateDownloadSources[sourceKey].Replace("{url}", fileUrl);
                        Console.WriteLine($@"源 {sourceKey} 测试成功，速度: {result.Speed:F2} B/s，开始下载");
                        
                        // 尝试设置结果，如果成功则取消其他测试
                        if (completionSource.TrySetResult((sourceKey, selectedUrl)))
                        {
                            cts.Cancel();
                            Console.WriteLine($@"使用第一个成功源: {sourceKey}");
                        }
                    }
                }
                catch (OperationCanceledException) when (sourceCts.Token.IsCancellationRequested)
                {
                    // 正常取消，忽略
                }
                catch (Exception ex)
                {
                    // 如果还没有成功结果，记录错误但不取消整体测试
                    if (!completionSource.Task.IsCompleted)
                    {
                        Console.WriteLine($@"源 {sourceKey} 测试失败: {ex.Message}");
                    }
                }
                finally
                {
                    // 清理
                    activeSources.TryRemove(sourceKey, out _);
                    sourceCts.Dispose();
                }
            }, sourceCts.Token));
        }

        try
        {
            // 添加一个超时任务，避免所有源都失败时无限等待
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds * 2), cts.Token);
            var completedTask = await Task.WhenAny(completionSource.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // 超时，取消所有测试
                cts.Cancel();
                throw new Exception($"所有下载源测试超时（超过{timeoutSeconds * 2}秒）");
            }
            
            // 等待一小段时间让其他任务有机会取消
            await Task.Delay(100, CancellationToken.None);
            
            return await completionSource.Task;
        }
        catch (Exception ex)
        {
            // 如果出现异常，先取消所有测试
            cts.Cancel();
            
            // 等待所有测试任务完成或取消
            try
            {
                await Task.WhenAll(testTasks);
            }
            catch
            {
                // 忽略所有取消异常
            }

            throw new Exception("所有下载源测试失败: " + ex.Message, ex);
        }
        finally
        {
            // 清理所有取消令牌源
            foreach (var sourceCts in activeSources.Values)
            {
                sourceCts.Dispose();
            }
            activeSources.Clear();
            cts.Dispose();
        }
    }
    
    /// <summary>
    /// 测试单个下载源速度
    /// </summary>
    private async Task<(string SourceName, double Speed, string Url)> TestSingleSourceAsync(
        string sourceName, 
        string sourcePattern, 
        string fileUrl,
        long testSize,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceUrl = sourcePattern.Replace("{url}", fileUrl)
                .Replace("{route}", fileUrl.Replace("https://github.com/", "").Replace("http://github.com/", ""));
            
            var uri = new Uri(sourceUrl);
            
            using var testClient = new HttpClient();
            testClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            testClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; DownloadSourceTester/1.0)");
            
            // 使用更快速的测试方法：只测试连接和少量数据
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 发送HEAD请求测试连接
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await testClient.SendAsync(headRequest, cancellationToken);
            
            if (!headResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($@"源 {sourceName} HEAD请求失败: {headResponse.StatusCode}");
                return (sourceName, 0, sourceUrl);
            }
            
            // 检查是否支持Range请求
            var supportsRange = headResponse.Headers.AcceptRanges?.ToString()
                .Equals("bytes", StringComparison.OrdinalIgnoreCase) == true;
            
            long testStart = 0;
            long testEnd = Math.Min(testSize - 1, headResponse.Content.Headers.ContentLength ?? testSize - 1);
            
            if (supportsRange && testEnd > testStart)
            {
                // 测试部分下载
                using var testRequest = new HttpRequestMessage(HttpMethod.Get, uri);
                testRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(testStart, testEnd);
                
                using var testResponse = await testClient.SendAsync(testRequest, cancellationToken);
                
                if (!testResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($@"源 {sourceName} Range请求失败: {testResponse.StatusCode}");
                    return (sourceName, 0, sourceUrl);
                }
                
                // 读取少量数据来测试实际下载速度
                var buffer = new byte[Math.Min(testSize, 1024 * 10)]; // 最多读取10KB来测试速度
                using var stream = await testResponse.Content.ReadAsStreamAsync(cancellationToken);
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                
                stopwatch.Stop();
                
                if (bytesRead == 0)
                {
                    Console.WriteLine($@"源 {sourceName} 未读取到数据");
                    return (sourceName, 0, sourceUrl);
                }
                
                // 计算速度（基于实际读取的字节数）
                var speed = bytesRead / stopwatch.Elapsed.TotalSeconds;
                return (sourceName, speed, sourceUrl);
            }
            else
            {
                // 如果不支持Range或者文件太小，使用GET请求测试
                using var testRequest = new HttpRequestMessage(HttpMethod.Get, uri);
                
                // 设置超时控制
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(3)); // 单个源测试最长3秒
                
                using var testResponse = await testClient.SendAsync(testRequest, timeoutCts.Token);
                
                if (!testResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($@"源 {sourceName} GET请求失败: {testResponse.StatusCode}");
                    return (sourceName, 0, sourceUrl);
                }
                
                // 读取少量数据来测试速度
                var buffer = new byte[Math.Min(testSize, 1024 * 10)];
                using var stream = await testResponse.Content.ReadAsStreamAsync(timeoutCts.Token);
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, timeoutCts.Token);
                
                stopwatch.Stop();
                
                if (bytesRead == 0)
                {
                    Console.WriteLine($@"源 {sourceName} 未读取到数据");
                    return (sourceName, 0, sourceUrl);
                }
                
                var speed = bytesRead / stopwatch.Elapsed.TotalSeconds;
                return (sourceName, speed, sourceUrl);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 正常取消，重新抛出以便上层处理
            throw;
        }
        catch (Exception ex)
        {
            // 不记录取消的异常
            if (!(ex is TaskCanceledException) && !(ex is OperationCanceledException))
            {
                // 只在调试时记录详细错误
                #if DEBUG
                Console.WriteLine($@"源 {sourceName} 测试失败: {ex.Message}");
                #endif
            }
            return (sourceName, 0, sourcePattern.Replace("{url}", fileUrl));
        }
    }
    
    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="fileUrl">原始Github文件URL</param>
    /// <param name="savePath">保存路径</param>
    /// <param name="progressCallback">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<bool> DownloadAsync(
        string fileUrl, 
        string savePath, 
        IProgress<DownloadProgress> progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("文件URL不能为空", nameof(fileUrl));
        
        if (string.IsNullOrWhiteSpace(savePath))
            throw new ArgumentException("保存路径不能为空", nameof(savePath));
        
        try
        {
            // 1. 并行测试所有下载源，使用第一个成功的源
            Console.WriteLine(@"开始并行测试下载源...");
            var (selectedSourceName, selectedUrl) = await TestDownloadSourcesAsync(fileUrl);
            
            Console.WriteLine($@"使用下载源: {selectedSourceName}");
            Console.WriteLine($@"下载URL: {selectedUrl}");
            
            // 2. 使用多线程下载器下载文件
            var result = await _downloader.DownloadAsync(
                selectedUrl, 
                savePath, 
                progressCallback, 
                cancellationToken);
            
            if (result)
            {
                Console.WriteLine($@"文件下载完成: {savePath}");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"下载失败: {ex.Message}");
            throw new Exception($"Github文件下载失败: {ex.Message}", ex);
        }
    }
}