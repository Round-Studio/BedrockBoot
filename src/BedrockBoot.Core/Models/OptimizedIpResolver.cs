using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace BedrockBoot.Core.Models;

public class OptimizedIpResolver
{
    private readonly HttpClient _httpClient;
    private readonly OptimizedIpConfig _config;

    public class OptimizedIpConfig
    {
        public string Domain { get; set; } = "count-bb.roundstudio.top";
        public int Port { get; set; } = 443;
        public string TestPath { get; set; } = "/";
        public int TimeoutSeconds { get; set; } = 3;
        public int CacheTtlSeconds { get; set; } = 300;
        public bool PreferIPv4 { get; set; } = true;
        public int MinResults { get; set; } = 3;
        public int MaxConcurrentTests { get; set; } = 10;
    }

    public class IpTestResult
    {
        public IPAddress IpAddress { get; set; }
        public TimeSpan Latency { get; set; }
        public bool IsSuccessful { get; set; }
        public string Error { get; set; }
    }

    public OptimizedIpResolver(OptimizedIpConfig config = null)
    {
        _config = config ?? new OptimizedIpConfig();

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            AllowAutoRedirect = false
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "BMCBL-Updater");
    }

    /// <summary>
    /// 获取最优 IP 地址
    /// </summary>
    public async Task<IPEndPoint> GetOptimizedIpAsync(bool useCache = true)
    {
        if (useCache)
        {
            var cached = await GetCachedOptimizedIp();
            if (cached != null)
                return cached;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. 解析域名获取所有 IP
            var ipAddresses = await ResolveDomainAsync(_config.Domain);
            if (!ipAddresses.Any())
            {
                Console.WriteLine($"未解析到 {_config.Domain} 的 IP 地址");
                return null;
            }

            Console.WriteLine($"解析到 {ipAddresses.Count} 个候选 IP");

            // 2. 根据优先级过滤
            var candidates = FilterByPreference(ipAddresses);

            // 3. 并发测试 IP 延迟
            var results = await TestIpLatencyAsync(candidates);

            // 4. 选择最优 IP
            var bestIp = SelectBestIp(results);

            if (bestIp != null)
            {
                Console.WriteLine($"✅ 优选IP竞速完成: {bestIp} (耗时: {stopwatch.ElapsedMilliseconds}ms)");
                await CacheOptimizedIp(bestIp);
            }

            return bestIp;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取优选IP失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 解析域名获取所有 IP 地址
    /// </summary>
    private async Task<List<IPAddress>> ResolveDomainAsync(string domain)
    {
        try
        {
            var entries = await Dns.GetHostEntryAsync(domain);
            return entries.AddressList.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DNS解析失败: {ex.Message}");

            // 备用解析方法
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(domain, _config.Port);
                var ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                return new List<IPAddress> { ip };
            }
            catch
            {
                return new List<IPAddress>();
            }
        }
    }

    /// <summary>
    /// 根据配置过滤 IP
    /// </summary>
    private List<IPAddress> FilterByPreference(List<IPAddress> addresses)
    {
        if (_config.PreferIPv4)
        {
            return addresses
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Concat(addresses.Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6))
                .ToList();
        }

        return addresses
            .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
            .Concat(addresses.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
            .ToList();
    }

    /// <summary>
    /// 并发测试 IP 延迟
    /// </summary>
    private async Task<List<IpTestResult>> TestIpLatencyAsync(List<IPAddress> ipAddresses)
    {
        var results = new List<IpTestResult>();
        var semaphore = new SemaphoreSlim(_config.MaxConcurrentTests);
        var tasks = new List<Task>();
        var cts = new CancellationTokenSource();

        foreach (var ip in ipAddresses.Take(_config.MaxConcurrentTests * 2))
        {
            await semaphore.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await TestSingleIpAsync(ip, cts.Token);
                    lock (results)
                    {
                        results.Add(result);

                        // 如果已经找到足够多的结果，可以取消其他测试
                        if (results.Count(r => r.IsSuccessful) >= _config.MinResults)
                        {
                            cts.Cancel();
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        return results;
    }

    /// <summary>
    /// 测试单个 IP 的延迟
    /// </summary>
    private async Task<IpTestResult> TestSingleIpAsync(IPAddress ip, CancellationToken cancellationToken)
    {
        var result = new IpTestResult { IpAddress = ip };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 方法1: TCP 连接测试
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(ip, _config.Port);

            if (await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(2), cancellationToken)) == connectTask)
            {
                await connectTask;

                // 方法2: HTTPS 请求测试
                var endpoint = new IPEndPoint(ip, _config.Port);
                var url = $"https://{_config.Domain}{_config.TestPath}";

                // 使用自定义 DNS 解析
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                // 通过 Host 头指定域名
                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Host = _config.Domain;
                client.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

                // 直接连接到指定 IP
                using var socketsHandler = new SocketsHttpHandler
                {
                    ConnectCallback = async (context, cancellationToken) =>
                    {
                        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        await socket.ConnectAsync(ip, _config.Port, cancellationToken);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                };

                using var httpClient = new HttpClient(socketsHandler);
                httpClient.DefaultRequestHeaders.Host = _config.Domain;
                httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

                var response = await httpClient.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    stopwatch.Stop();
                    result.IsSuccessful = true;
                    result.Latency = stopwatch.Elapsed;

                    Console.WriteLine($"  ✓ {ip}: {stopwatch.ElapsedMilliseconds}ms");
                }
                else
                {
                    throw new Exception($"HTTP状态码: {response.StatusCode}");
                }
            }
            else
            {
                throw new TimeoutException("TCP连接超时");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsSuccessful = false;
            result.Error = ex.Message;
            Console.WriteLine($"  ✗ {ip}: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 选择最优 IP
    /// </summary>
    private IPEndPoint SelectBestIp(List<IpTestResult> results)
    {
        var successful = results
            .Where(r => r.IsSuccessful)
            .OrderBy(r => r.Latency)
            .ToList();

        if (!successful.Any())
            return null;

        var best = successful.First();
        return new IPEndPoint(best.IpAddress, _config.Port);
    }

    // 简单的内存缓存
    private static (IPEndPoint Endpoint, DateTime Expiry) _cache;

    private async Task<IPEndPoint> GetCachedOptimizedIp()
    {
        if (_cache.Endpoint != null && DateTime.Now < _cache.Expiry)
        {
            Console.WriteLine($"使用缓存的优选IP: {_cache.Endpoint}");
            return _cache.Endpoint;
        }

        return null;
    }

    private async Task CacheOptimizedIp(IPEndPoint endpoint)
    {
        _cache = (endpoint, DateTime.Now.AddSeconds(_config.CacheTtlSeconds));
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}