using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockBoot.Core.Models;

public class OptimizedIpResolver : IDisposable
{
    private readonly OptimizedIpConfig _config;
    // 使用单例 Handler 避免 Socket 泄露
    private readonly SocketsHttpHandler _sharedHandler;

    public class OptimizedIpConfig
    {
        public string Domain { get; set; } = "count-bb.roundstudio.top";
        public int Port { get; set; } = 443;
        public string TestPath { get; set; } = "/";
        public int TimeoutSeconds { get; set; } = 3;
        public int CacheTtlSeconds { get; set; } = 300;
        public bool PreferIPv4 { get; set; } = true;
        public int MinResults { get; set; } = 2; // 找到2个足够快的就停，提高效率
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
        
        _sharedHandler = new SocketsHttpHandler
        {
            SslOptions = { RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true },
            AllowAutoRedirect = false,
            ConnectTimeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
    }

    public async Task<IPEndPoint> GetOptimizedIpAsync(bool useCache = true)
    {
        if (useCache)
        {
            var cached = await GetCachedOptimizedIp();
            if (cached != null) return cached;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var ipAddresses = await ResolveDomainAsync(_config.Domain);
            if (!ipAddresses.Any()) return null;

            var candidates = FilterByPreference(ipAddresses);
            var results = await TestIpLatencyAsync(candidates);
            var bestIp = SelectBestIp(results);

            if (bestIp != null)
            {
                await CacheOptimizedIp(bestIp);
            }

            return bestIp;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取优选IP流程异常: {ex.Message}");
            return null;
        }
    }

    private async Task<List<IPAddress>> ResolveDomainAsync(string domain)
    {
        try
        {
            return (await Dns.GetHostAddressesAsync(domain)).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DNS解析失败: {ex.Message}");
            return new List<IPAddress>();
        }
    }

    private List<IPAddress> FilterByPreference(List<IPAddress> addresses)
    {
        var ipv4 = addresses.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        var ipv6 = addresses.Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6);

        return _config.PreferIPv4 
            ? ipv4.Concat(ipv6).ToList() 
            : ipv6.Concat(ipv4).ToList();
    }

    private async Task<List<IpTestResult>> TestIpLatencyAsync(List<IPAddress> ipAddresses)
    {
        var results = new List<IpTestResult>();
        using var semaphore = new SemaphoreSlim(_config.MaxConcurrentTests);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds + 2));
        var tasks = new List<Task>();

        foreach (var ip in ipAddresses.Take(20))
        {
            await semaphore.WaitAsync(cts.Token);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    if (cts.IsCancellationRequested) return;

                    var result = await TestSingleIpAsync(ip, cts.Token);
                    
                    lock (results)
                    {
                        results.Add(result);
                        if (result.IsSuccessful && results.Count(r => r.IsSuccessful) >= _config.MinResults)
                        {
                            cts.Cancel(); // 达到目标，触发其他任务中止
                        }
                    }
                }
                catch (OperationCanceledException) { /* 忽略信号触发的取消 */ }
                finally
                {
                    semaphore.Release();
                }
            }, cts.Token));
        }

        try { await Task.WhenAll(tasks); } catch { /* 忽略WhenAll中的取消异常 */ }
        return results;
    }

    private async Task<IpTestResult> TestSingleIpAsync(IPAddress ip, CancellationToken ct)
    {
        var result = new IpTestResult { IpAddress = ip };
        var sw = Stopwatch.StartNew();

        try
        {
            // 使用 Socket 替代 TcpClient，以获得更好的 CancellationToken 支持
            using var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            // 步骤 1: TCP 连接竞速
            await socket.ConnectAsync(new IPEndPoint(ip, _config.Port), ct);

            // 步骤 2: HTTPS 应用层测试 (复用 Handler)
            using var client = new HttpClient(new SocketsHttpHandler
            {
                ConnectCallback = async (context, token) =>
                {
                    // 此处不再重连，但由于 HttpClient 设计，我们需要提供流
                    // 为简单起见，这里直接利用之前的连接或让 HttpClient 重新在特定 IP 上握手
                    var s = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    await s.ConnectAsync(new IPEndPoint(ip, _config.Port), token);
                    return new NetworkStream(s, ownsSocket: true);
                },
                SslOptions = { RemoteCertificateValidationCallback = delegate { return true; } }
            }) 
            { 
                Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds) 
            };

            client.DefaultRequestHeaders.Host = _config.Domain;
            var url = $"https://{_config.Domain}{_config.TestPath}";
            
            var response = await client.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                sw.Stop();
                result.IsSuccessful = true;
                result.Latency = sw.Elapsed;
                Console.WriteLine($"  ✓ {ip}: {sw.ElapsedMilliseconds}ms");
            }
        }
        catch (OperationCanceledException)
        {
            result.IsSuccessful = false;
            result.Error = "Task Cancelled";
            // 不在控制台打印取消的错误，减少噪音
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.IsSuccessful = false;
            result.Error = ex.Message;
            Console.WriteLine($"  ✗ {ip}: {ex.Message}");
        }

        return result;
    }

    private IPEndPoint SelectBestIp(List<IpTestResult> results)
    {
        var best = results
            .Where(r => r.IsSuccessful)
            .OrderBy(r => r.Latency)
            .FirstOrDefault();

        return best != null ? new IPEndPoint(best.IpAddress, _config.Port) : null;
    }

    private static (IPEndPoint Endpoint, DateTime Expiry) _cache;

    private async Task<IPEndPoint> GetCachedOptimizedIp()
    {
        if (_cache.Endpoint != null && DateTime.Now < _cache.Expiry)
            return _cache.Endpoint;
        return null;
    }

    private async Task CacheOptimizedIp(IPEndPoint endpoint)
    {
        _cache = (endpoint, DateTime.Now.AddSeconds(_config.CacheTtlSeconds));
    }

    public void Dispose()
    {
        _sharedHandler?.Dispose();
    }
}