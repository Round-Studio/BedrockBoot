using System;
using System.Runtime.InteropServices;
using System.Web;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace BedrockBoot.Core.Models
{
    public class AnalyticsService : IDisposable
    {
        private static HttpClient _httpClient;
        private const string BaseUrl = "https://count-bb.roundstudio.top/push";
        private const string Domain = "count-bb.roundstudio.top";
        
        private static OptimizedIpResolver _ipResolver;
        private static IPEndPoint _optimizedEndpoint;
        private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        static AnalyticsService()
        {
            InitializeResolver();
        }

        private static void InitializeResolver()
        {
            var config = new OptimizedIpResolver.OptimizedIpConfig
            {
                Domain = Domain,
                Port = 443,
                TimeoutSeconds = 3,
                CacheTtlSeconds = 300, // 5分钟缓存
                MinResults = 2,
                MaxConcurrentTests = 10,
                PreferIPv4 = true,
                TestPath = "/" // 测试根路径
            };

            _ipResolver = new OptimizedIpResolver(config);
        }

        /// <summary>
        /// 确保HttpClient已初始化（使用优选IP）
        /// </summary>
        private static async Task EnsureHttpClientAsync()
        {
            if (_httpClient != null) 
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_httpClient == null)
                {
                    // 获取优选IP
                    _optimizedEndpoint = await _ipResolver.GetOptimizedIpAsync(useCache: true);
                    
                    if (_optimizedEndpoint != null)
                    {
                        Console.WriteLine($@"使用优选IP: {_optimizedEndpoint}");
                        _httpClient = CreateOptimizedHttpClient(_optimizedEndpoint);
                    }
                    else
                    {
                        // 如果优选失败，使用普通HTTP客户端
                        Console.WriteLine(@"优选IP获取失败，使用普通连接");
                        _httpClient = CreateDefaultHttpClient();
                    }
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// 创建使用优选IP的HttpClient
        /// </summary>
        private static HttpClient CreateOptimizedHttpClient(IPEndPoint endpoint)
        {
            var socketsHandler = new SocketsHttpHandler
            {
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    
                    try
                    {
                        // 设置KeepAlive
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        
                        // 连接到优选IP
                        await socket.ConnectAsync(endpoint.Address, endpoint.Port, cancellationToken)
                            .ConfigureAwait(false);
                        
                        Console.WriteLine($@"已连接到优选IP: {endpoint.Address}:{endpoint.Port}");
                        
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                },
                
                // 连接池优化
                MaxConnectionsPerServer = 10,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                
                // 超时设置
                ConnectTimeout = TimeSpan.FromSeconds(5),
                Expect100ContinueTimeout = TimeSpan.FromSeconds(2)
            };

            var client = new HttpClient(socketsHandler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            // 设置默认请求头
            client.DefaultRequestHeaders.Host = Domain;
            client.DefaultRequestHeaders.Add("User-Agent", "BedrockBoot-Analytics/1.0");
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            
            // 添加缓存控制
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

            return client;
        }

        /// <summary>
        /// 创建默认HttpClient（备选方案）
        /// </summary>
        private static HttpClient CreateDefaultHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            client.DefaultRequestHeaders.Add("User-Agent", "BedrockBoot-Analytics/1.0");
            client.DefaultRequestHeaders.Add("Accept", "*/*");

            return client;
        }

        /// <summary>
        /// 刷新优选IP（可定期调用）
        /// </summary>
        public static async Task<bool> RefreshOptimizedIpAsync()
        {
            try
            {
                Console.WriteLine(@"开始刷新优选IP...");
                
                var newEndpoint = await _ipResolver.GetOptimizedIpAsync(useCache: false);
                
                if (newEndpoint != null)
                {
                    await _initLock.WaitAsync();
                    try
                    {
                        _optimizedEndpoint = newEndpoint;
                        
                        // 重新创建HttpClient
                        var oldClient = _httpClient;
                        _httpClient = CreateOptimizedHttpClient(newEndpoint);
                        
                        // 释放旧客户端
                        oldClient?.Dispose();
                        
                        Console.WriteLine($@"优选IP刷新成功: {newEndpoint}");
                        return true;
                    }
                    finally
                    {
                        _initLock.Release();
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"刷新优选IP失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 推送设备日志（使用优选IP）
        /// </summary>
        public static async Task<bool> PushDeviceLog(string version)
        {
            try
            {
                // 确保HttpClient已初始化
                await EnsureHttpClientAsync();

                // 获取设备信息
                string deviceName = Environment.MachineName;
                string machineCode = GetMachineCode();
                string user = $"{deviceName}_{machineCode}";
                string system = GetOperatingSystemInfo();
                string type = "BedrockBoot";

                // 构建URL
                var builder = new UriBuilder(BaseUrl);
                var query = HttpUtility.ParseQueryString(string.Empty);
                query["user"] = user;
                query["system"] = system;
                query["version"] = version;
                query["type"] = type;
                query["t"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(); // 添加时间戳避免缓存
                builder.Query = query.ToString();

                var url = builder.ToString();
                
                // 记录使用的IP信息
                if (_optimizedEndpoint != null)
                {
                    Console.WriteLine($@"使用优选IP {_optimizedEndpoint.Address} 发送请求");
                }

                // 发送请求
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Forwarded-For", GetLocalIpAddress());
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($@"日志推送成功: {user}");
                    return true;
                }
                else
                {
                    Console.WriteLine($@"日志推送失败: {response.StatusCode}");
                    
                    // 如果失败且可能是网络问题，尝试刷新优选IP
                    if (response.StatusCode == HttpStatusCode.RequestTimeout || 
                        response.StatusCode == HttpStatusCode.GatewayTimeout ||
                        (int)response.StatusCode >= 500)
                    {
                        _ = Task.Run(async () => await RefreshOptimizedIpAsync());
                    }
                    
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($@"网络请求异常: {ex.Message}");
                
                // 网络异常时尝试刷新优选IP
                _ = Task.Run(async () => await RefreshOptimizedIpAsync());
                
                return false;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($@"请求超时: {ex.Message}");
                
                // 超时时尝试刷新优选IP
                _ = Task.Run(async () => await RefreshOptimizedIpAsync());
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"推送异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取本地IP地址（用于X-Forwarded-For）
        /// </summary>
        private static string GetLocalIpAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        // 原有的机器码获取方法保持不变
        private static string GetMachineCode()
        {
            try
            {
                string cpuId = GetCpuId();
                string diskId = GetDiskId();
                string macAddress = GetMacAddress();
                
                string combined = $"{cpuId}{diskId}{macAddress}";
                
                if (string.IsNullOrWhiteSpace(combined))
                {
                    combined = Environment.MachineName + Environment.ProcessorCount + Environment.OSVersion.VersionString;
                }
                
                using (MD5 md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
                }
            }
            catch
            {
                return GenerateFallbackMachineCode();
            }
        }

        private static string GetCpuId()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["ProcessorId"]?.ToString() ?? "";
                }
            }
            catch { }
            return "";
        }

        private static string GetDiskId()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index=0");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["SerialNumber"]?.ToString().Trim() ?? "";
                }
            }
            catch { }
            return "";
        }

        private static string GetMacAddress()
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up && 
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        !nic.Description.ToLower().Contains("virtual") &&
                        !nic.Description.ToLower().Contains("pseudo"))
                    {
                        return nic.GetPhysicalAddress().ToString();
                    }
                }
            }
            catch { }
            return "";
        }

        private static string GenerateFallbackMachineCode()
        {
            try
            {
                string fallback = Environment.MachineName + 
                                 Environment.ProcessorCount + 
                                 Environment.OSVersion.VersionString +
                                 Environment.UserDomainName;
                
                using (MD5 md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(fallback));
                    return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
                }
            }
            catch
            {
                return Guid.NewGuid().ToString("N").Substring(0, 16);
            }
        }

        private static string GetOperatingSystemInfo()
        {
            string osDescription = RuntimeInformation.OSDescription;
            string osArchitecture = RuntimeInformation.OSArchitecture.ToString();

            if (osDescription.Contains("Windows"))
            {
                int build = Environment.OSVersion.Version.Build;
                if (osDescription.Contains("10.0") && build >= 22000)
                {
                    return $"Windows 11.{GetOSBuildNumber()}";
                }
                else if (osDescription.Contains("10.0"))
                {
                    return $"Windows 10.{GetOSBuildNumber()}";
                }
            }

            return $"{osDescription} ({osArchitecture})";
        }

        private static string GetOSBuildNumber()
        {
            try
            {
                var version = Environment.OSVersion.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                return "Unknown";
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // _httpClient / _ipResolver / _initLock 均为静态资源，随进程生命周期存活。
                // 不能在实例 Dispose 中释放，否则后续所有静态调用会抛 ObjectDisposedException。
                _disposed = true;
            }
        }
    }
}