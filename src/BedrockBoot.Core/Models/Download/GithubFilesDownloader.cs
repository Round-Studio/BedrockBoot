using System.Collections.Concurrent;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Models.Global;

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
    private async Task<(string SourceName, string Url)> TestDownloadSourcesAsync(
        string fileUrl,
        long testSize = 1024 * 512,
        int timeoutSeconds = 20)
    {
        var cts = new CancellationTokenSource();
        var completionSource = new TaskCompletionSource<(string SourceName, string Url)>();
        var testTasks = new List<Task>();
        var activeSources = new ConcurrentDictionary<string, CancellationTokenSource>();
        var githubSourceFound = false;

        foreach (var source in SourceList.UpdateDownloadSources)
        {
            var sourceKey = source.Key;
            var sourcePattern = source.Value;
            var sourceCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);

            activeSources[sourceKey] = sourceCts;

            // 如果是 GitHub 源，标记一下
            if (sourceKey == "Github")
            {
                githubSourceFound = true;
            }

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
                        var selectedUrl = SourceList.UpdateDownloadSources[sourceKey].Replace("{url}", fileUrl);
                        Console.WriteLine($@"源 {sourceKey} 测试成功，速度: {result.Speed:F2} B/s，开始下载");

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
                    if (!completionSource.Task.IsCompleted)
                    {
                        Console.WriteLine($@"源 {sourceKey} 测试失败: {ex.Message}");
                    }
                }
                finally
                {
                    activeSources.TryRemove(sourceKey, out _);
                    sourceCts.Dispose();
                }
            }, sourceCts.Token));
        }

        try
        {
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds * 2), cts.Token);
            var completedTask = await Task.WhenAny(completionSource.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                cts.Cancel();

                // 所有加速源都超时，使用 GitHub 原始源
                Console.WriteLine($@"所有加速源测试超时，使用 GitHub 原始源");
                var githubUrl = SourceList.UpdateDownloadSources["Github"].Replace("{url}", fileUrl);
                return ("Github (fallback)", githubUrl);
            }

            await Task.Delay(100, CancellationToken.None);
            return await completionSource.Task;
        }
        catch (Exception ex)
        {
            cts.Cancel();

            try
            {
                await Task.WhenAll(testTasks);
            }
            catch
            {
                // 忽略所有取消异常
            }

            // 如果所有测试都失败了，使用 GitHub 原始源
            Console.WriteLine($@"所有加速源测试失败: {ex.Message}，使用 GitHub 原始源");
            var githubUrl = SourceList.UpdateDownloadSources["Github"].Replace("{url}", fileUrl);
            return ("Github (fallback)", githubUrl);
        }
        finally
        {
            foreach (var sourceCts in activeSources.Values)
            {
                sourceCts.Dispose();
            }

            activeSources.Clear();
            cts.Dispose();
        }
    }

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

            // 对 GitHub 源使用更长的超时时间
            var actualTimeout = sourceName == "Github" ? timeoutSeconds * 2 : timeoutSeconds;
            testClient.Timeout = TimeSpan.FromSeconds(actualTimeout);
            testClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; DownloadSourceTester/1.0)");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            using var headRequest = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await testClient.SendAsync(headRequest, cancellationToken);

            if (!headResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($@"源 {sourceName} HEAD请求失败: {headResponse.StatusCode}");
                return (sourceName, 0, sourceUrl);
            }

            var supportsRange = headResponse.Headers.AcceptRanges?.ToString()
                .Equals("bytes", StringComparison.OrdinalIgnoreCase) == true;

            long testStart = 0;
            long testEnd = Math.Min(testSize - 1, headResponse.Content.Headers.ContentLength ?? testSize - 1);

            if (supportsRange && testEnd > testStart)
            {
                using var testRequest = new HttpRequestMessage(HttpMethod.Get, uri);
                testRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(testStart, testEnd);

                using var testResponse = await testClient.SendAsync(testRequest, cancellationToken);

                if (!testResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($@"源 {sourceName} Range请求失败: {testResponse.StatusCode}");
                    return (sourceName, 0, sourceUrl);
                }

                var buffer = new byte[Math.Min(testSize, 1024 * 10)];
                using var stream = await testResponse.Content.ReadAsStreamAsync(cancellationToken);
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                stopwatch.Stop();

                if (bytesRead == 0)
                {
                    Console.WriteLine($@"源 {sourceName} 未读取到数据");
                    return (sourceName, 0, sourceUrl);
                }

                var speed = bytesRead / stopwatch.Elapsed.TotalSeconds;
                return (sourceName, speed, sourceUrl);
            }
            else
            {
                using var testRequest = new HttpRequestMessage(HttpMethod.Get, uri);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(sourceName == "Github" ? 10 : 3));

                using var testResponse = await testClient.SendAsync(testRequest, timeoutCts.Token);

                if (!testResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($@"源 {sourceName} GET请求失败: {testResponse.StatusCode}");
                    return (sourceName, 0, sourceUrl);
                }

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
            throw;
        }
        catch (Exception ex)
        {
            if (!(ex is TaskCanceledException) && !(ex is OperationCanceledException))
            {
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