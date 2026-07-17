using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;
using static System.Reflection.BindingFlags;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;

public class CurseForgeApiClient : IDisposable
{
    private static readonly object _lock = new();
    private readonly string _apiKey;
    
    // 使用 volatile 确保多线程可见性
    private volatile HttpClient _sharedHttpClient;
    private volatile HttpClient _fixedSourceHttpClient; 
    
    // 统一获取 User-Agent 字符串
    private string UserAgent => $"BedrockBoot/{Global.GlobalModel.BodyVersion}";

    public CurseForgeApiClient(string apiKey)
    {
        _apiKey = apiKey;
        _sharedHttpClient = CreateHttpClient(GetCurrentBaseAddress());
        _fixedSourceHttpClient = CreateHttpClient(GetFixedBaseAddress());
    }

    private HttpClient CreateHttpClient(Uri baseAddress)
    {
        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                AllowRenegotiation = true,
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    return sslPolicyErrors == SslPolicyErrors.None;
                }
            },
            ConnectTimeout = TimeSpan.FromSeconds(15),
            PooledConnectionLifetime = TimeSpan.FromMinutes(1),
            MaxConnectionsPerServer = 10,
            UseProxy = true,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 3,
            PreAuthenticate = false,
            // 关键修复：显式启用自动解压
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30),
            BaseAddress = baseAddress,
            DefaultRequestVersion = HttpVersion.Version20
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // 注意：如果启用了 AutomaticDecompression，通常不需要手动添加 Accept-Encoding，
        // 但保留它也没坏处，Handler 会处理。
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

        return client;
    }

    /// <summary>
    /// 获取当前选择的源地址
    /// </summary>
    private Uri GetCurrentBaseAddress()
    {
        var sourceList = SourceList.CurseForgeSource.Values.ToList();
        var index = GlobalModel.Config.Data.CurseForgeSourceIndex;
        
        // 确保索引在有效范围内
        if (index < 0 || index >= sourceList.Count)
        {
            index = 0;
            GlobalModel.Config.Data.CurseForgeSourceIndex = index;
            GlobalModel.Config.Save();
        }
        
        return new Uri(sourceList[index]);
    }

    /// <summary>
    /// 获取固定第一个源的地址（索引0）
    /// </summary>
    private Uri GetFixedBaseAddress()
    {
        return new Uri(SourceList.CurseForgeSource.Values.ToList()[0]);
    }

    /// <summary>
    /// 刷新共享 HttpClient
    /// </summary>
    private void RefreshSharedHttpClient()
    {
        lock (_lock)
        {
            var oldClient = _sharedHttpClient;
            _sharedHttpClient = CreateHttpClient(GetCurrentBaseAddress());
            oldClient?.Dispose();
        }
    }

    /// <summary>
    /// 刷新固定源 HttpClient
    /// </summary>
    private void RefreshFixedSourceHttpClient()
    {
        lock (_lock)
        {
            var oldClient = _fixedSourceHttpClient;
            _fixedSourceHttpClient = CreateHttpClient(GetFixedBaseAddress());
            oldClient?.Dispose();
        }
    }

    /// <summary>
    /// 获取当前共享 HttpClient（自动检查配置变更）
    /// </summary>
    private HttpClient GetSharedHttpClient()
    {
        var currentAddress = _sharedHttpClient.BaseAddress?.ToString();
        var expectedAddress = GetCurrentBaseAddress().ToString();
        
        if (currentAddress != expectedAddress)
        {
            RefreshSharedHttpClient();
        }
        
        return _sharedHttpClient;
    }

    /// <summary>
    /// 执行带有重试逻辑的 HTTP 请求
    /// </summary>
    private async Task<T> ExecuteRequestAsync<T>(
        Func<HttpClient, HttpRequestMessage> requestFactory,
        Func<string, T> deserializeFunc,
        string operationName,
        bool useFixedSource = false)
    {
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount <= maxRetries)
        {
            try
            {
                var client = useFixedSource ? _fixedSourceHttpClient : GetSharedHttpClient();
                
                using var request = requestFactory(client);
                request.Headers.Add("x-api-key", _apiKey);

                // 发送请求
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // 关键修复：确保正确读取字符串，处理可能的压缩流
                // 虽然 AutomaticDecompression 应该处理了，但 ReadAsStringAsync 是最安全的方式
                var json = await response.Content.ReadAsStringAsync();
                
                // 调试用：如果仍然出错，可以取消注释下面这行查看原始字节的前几个字节
                // if (json.Length > 0 && json[0] == 0x1F) throw new Exception("Received Gzip data despite decompression settings");

                return deserializeFunc(json);
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries && IsRetryableError(ex))
            {
                retryCount++;
                Console.WriteLine($"[{operationName}] 错误 (重试 {retryCount}/{maxRetries}): {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"内部异常: {ex.InnerException.Message}");

                // 策略：如果是 SSL 错误，重建对应的 HttpClient
                if (IsSslError(ex))
                {
                    if (useFixedSource)
                        RefreshFixedSourceHttpClient();
                    else
                        RefreshSharedHttpClient();
                }

                // 指数退避等待
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                await Task.Delay(delay);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[{operationName}] JSON解析错误: {ex.Message}");
                // 如果是由于压缩问题导致的，可以尝试重新创建客户端再重试一次
                if (retryCount < maxRetries)
                {
                     if (useFixedSource)
                        RefreshFixedSourceHttpClient();
                    else
                        RefreshSharedHttpClient();
                     
                     retryCount++;
                     await Task.Delay(TimeSpan.FromSeconds(1));
                     continue;
                }
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[{operationName}] 请求超时: {ex.Message}");
                throw new Exception("请求超时，请检查网络连接或稍后重试", ex);
            }
        }

        throw new HttpRequestException($"[{operationName}] 在重试 {maxRetries} 次后仍然无法建立连接");
    }

    private bool IsRetryableError(HttpRequestException ex)
    {
        var msg = ex.Message.ToLowerInvariant();
        return msg.Contains("ssl connection could not be established") ||
               msg.Contains("remote host closed") ||
               msg.Contains("connection was closed") ||
               msg.Contains("unable to read data from the transport connection");
    }

    private bool IsSslError(HttpRequestException ex)
    {
        return ex.Message.Contains("SSL connection could not be established") ||
               (ex.InnerException is IOException && ex.InnerException.Message.Contains("remote host"));
    }

    /// <summary>
    /// 获取指定 modId 的详细信息
    /// </summary>
    public async Task<CurseForgeResponse.ModData> GetModDetailsAsync(int modId)
    {
        return await ExecuteRequestAsync(
            client => new HttpRequestMessage(HttpMethod.Get, $"v1/mods/{modId}"),
            json =>
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return JsonSerializer.Deserialize<CurseForgeResponse.SingleModResponse>(json, options)?.Data;
            },
            "GetModDetails"
        );
    }

    public async Task<CurseForgeResponse> SearchModsAsync(
        string searchFilter,
        int pageSize = 20,
        string gameVersion = null,
        int? classId = null,
        int? index = null,
        string modLoader = null)
    {
        var queryParams = new StringBuilder();
        queryParams.Append($"?gameId={78022}");

        if (!string.IsNullOrEmpty(searchFilter))
            queryParams.Append($"&searchFilter={Uri.EscapeDataString(searchFilter)}");

        queryParams.Append($"&pageSize={pageSize}");

        if (!string.IsNullOrEmpty(gameVersion))
            queryParams.Append($"&gameVersion={Uri.EscapeDataString(gameVersion)}");

        if (classId.HasValue)
            queryParams.Append($"&classId={classId.Value}");

        if (index.HasValue)
            queryParams.Append($"&index={index.Value}");

        if (!string.IsNullOrEmpty(modLoader))
            queryParams.Append($"&modLoader={Uri.EscapeDataString(modLoader)}");

        var url = $"v1/mods/search{queryParams}&sortOrder=desc";

        return await ExecuteRequestAsync(
            client => new HttpRequestMessage(HttpMethod.Get, url),
            json =>
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return JsonSerializer.Deserialize<CurseForgeResponse>(json, options);
            },
            "SearchMods"
        );
    }

    /// <summary>
    ///     获取推荐的模组（使用固定源 - 索引0）
    /// </summary>
    public async Task<CurseForgeFeaturedResponse> GetFeaturedModsAsync(int gameId = 78022)
    {
        var requestBody = new { gameId };
        var jsonBody = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        return await ExecuteRequestAsync(
            client =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "v1/mods/featured")
                {
                    Content = content
                };
                return request;
            },
            json =>
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return JsonSerializer.Deserialize<CurseForgeFeaturedResponse>(json, options);
            },
            "GetFeaturedMods",
            useFixedSource: true
        );
    }

    /// <summary>
    ///     获取指定 modId 的所有文件
    /// </summary>
    public async Task<CurseForgeResponse.ModFilesResponse> GetModFilesAsync(
        int modId,
        int pageSize = 50,
        int? index = null,
        string gameVersion = null)
    {
        return await ExecuteRequestAsync(
            client => new HttpRequestMessage(HttpMethod.Get, $"v1/mods/{modId}/files"),
            json =>
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return JsonSerializer.Deserialize<CurseForgeResponse.ModFilesResponse>(json, options);
            },
            "GetModFiles"
        );
    }

    /// <summary>
    ///     获取指定文件ID的详细信息
    /// </summary>
    public async Task<CurseForgeResponse.ModFile> GetFileDetailsAsync(int fileId)
    {
        return await ExecuteRequestAsync(
            client => new HttpRequestMessage(HttpMethod.Get, $"v1/mods/files/{fileId}"),
            json =>
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return JsonSerializer.Deserialize<CurseForgeResponse.SingleFileResponse>(json, options)?.Data;
            },
            "GetFileDetails"
        );
    }

    /// <summary>
    ///     获取多个文件的详细信息
    /// </summary>
    public async Task<List<CurseForgeResponse.ModFile>> GetMultipleFilesAsync(int[] fileIds)
    {
        var requestBody = new { fileIds };
        var jsonBody = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        return await ExecuteRequestAsync(
            client =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "v1/mods/files")
                {
                    Content = content
                };
                return request;
            },
            json =>
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var response = JsonSerializer.Deserialize<CurseForgeResponse.ModFilesResponse>(json, options);
                return response?.Data ?? new List<CurseForgeResponse.ModFile>();
            },
            "GetMultipleFiles"
        );
    }

    /// <summary>
    ///     获取指定 modId 的 Markdown 格式描述（使用固定源 - 索引0）
    /// </summary>
    public async Task<string> GetModDescriptionAsync(int modId)
    {
        return await ExecuteRequestAsync(
            client => new HttpRequestMessage(HttpMethod.Get, $"v1/mods/{modId}/description"),
            json =>
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var response = JsonSerializer.Deserialize<CurseForgeResponse.ModDescriptionResponse>(json, options);
                return response?.Data ?? string.Empty;
            },
            "GetModDescription",
            useFixedSource: true
        );
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _sharedHttpClient?.Dispose();
        _fixedSourceHttpClient?.Dispose();
    }
}