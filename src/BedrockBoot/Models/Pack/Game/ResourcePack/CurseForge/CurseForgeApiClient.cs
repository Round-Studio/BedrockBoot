using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;

public class CurseForgeApiClient
{
    private static readonly object _lock = new();
    private readonly string _apiKey;
    private HttpClient _sharedHttpClient;
    private HttpClient _fixedSourceHttpClient; // 新增：固定使用第一个源的 HttpClient
    
    // 统一获取 User-Agent 字符串
    private string UserAgent => $"BedrockBoot/{Global.GlobalModel.BodyVersion}";

    public CurseForgeApiClient(string apiKey)
    {
        _apiKey = apiKey;
        InitializeHttpClient();
        InitializeFixedSourceHttpClient(); // 初始化固定源的 HttpClient
    }

    private void InitializeHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            // 配置SSL/TLS选项
            SslOptions = new SslClientAuthenticationOptions
            {
                // 启用所有TLS版本，让服务器选择
                EnabledSslProtocols = SslProtocols.Tls12 |
                                      SslProtocols.Tls13 |
                                      SslProtocols.Tls11 |
                                      SslProtocols.Tls
            },
            ConnectTimeout = TimeSpan.FromSeconds(30),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2), // 缩短连接生命周期
            MaxConnectionsPerServer = 5, // 限制每个服务器的连接数
            UseProxy = true,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 3
        };

        _sharedHttpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(60), // 增加超时时间
            BaseAddress = new Uri(SourceList.CurseForgeSource.Values.ToList()[GlobalModel.Config.Data.CurseForgeSourceIndex]),
            DefaultRequestVersion = HttpVersion.Version20
        };

        // 设置默认请求头 - 使用统一的 User-Agent
        _sharedHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        _sharedHttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// 初始化固定使用第一个源的 HttpClient（用于获取推荐和描述）
    /// </summary>
    private void InitializeFixedSourceHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 |
                                      SslProtocols.Tls13 |
                                      SslProtocols.Tls11 |
                                      SslProtocols.Tls
            },
            ConnectTimeout = TimeSpan.FromSeconds(30),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 5,
            UseProxy = true,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 3
        };

        // 固定使用第一个源（索引 0）
        var fixedBaseAddress = new Uri(SourceList.CurseForgeSource.Values.ToList()[0]);
        
        _fixedSourceHttpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(60),
            BaseAddress = fixedBaseAddress,
            DefaultRequestVersion = HttpVersion.Version20
        };

        // 设置默认请求头
        _fixedSourceHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        _fixedSourceHttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // 重新初始化HttpClient的方法，用于处理连接问题
    private void ReinitializeHttpClient()
    {
        lock (_lock)
        {
            _sharedHttpClient?.Dispose();
            InitializeHttpClient();
        }
    }

    // 重新初始化固定源HttpClient的方法
    private void ReinitializeFixedSourceHttpClient()
    {
        lock (_lock)
        {
            _fixedSourceHttpClient?.Dispose();
            InitializeFixedSourceHttpClient();
        }
    }

    /// <summary>
    ///     获取指定 modId 的详细信息
    /// </summary>
    /// <param name="modId">模组ID</param>
    /// <returns>模组详细信息</returns>
    public async Task<CurseForgeResponse.ModData> GetModDetailsAsync(int modId)
    {
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount <= maxRetries)
            try
            {
                var url = $"v1/mods/{modId}";

                // 创建请求
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", _apiKey);

                var response = await _sharedHttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var modResponse = JsonSerializer.Deserialize<CurseForgeResponse.SingleModResponse>(json, options);
                return modResponse?.Data;
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries &&
                                                  (ex.Message.Contains("SSL connection could not be established") ||
                                                   ex.Message.Contains("remote host closed") ||
                                                   ex.Message.Contains("Connection was closed")))
            {
                retryCount++;
                Console.WriteLine($@"获取模组详情错误 (重试 {retryCount}/{maxRetries}): {ex}");

                // 如果是SSL连接问题，重新初始化HttpClient
                if (ex.Message.Contains("SSL connection could not be established"))
                    ReinitializeHttpClient();

                // 等待一段时间后重试
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount))); // 指数退避
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($@"获取模组详情错误: {ex}");
                if (ex.InnerException != null)
                    Console.WriteLine($@"内部异常: {ex.InnerException.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($@"JSON解析错误: {ex}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($@"请求超时: {ex}");
                throw new Exception("请求超时，请检查网络连接或稍后重试", ex);
            }

        // 如果重试后仍然失败，抛出异常
        throw new HttpRequestException($"在重试{maxRetries}次后仍然无法建立连接");
    }

    public async Task<CurseForgeResponse> SearchModsAsync(
        string searchFilter,
        int gameId = 78022,
        int pageSize = 20,
        string gameVersion = null,
        int? classId = null,
        int? index = null,
        string modLoader = null)
    {
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount <= maxRetries)
            try
            {
                // 构建参数
                var queryParams = new StringBuilder();
                queryParams.Append($"?gameId={gameId}");

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

                // 创建请求
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", _apiKey);

                var response = await _sharedHttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var curseForgeResponse = JsonSerializer.Deserialize<CurseForgeResponse>(json, options);
                return curseForgeResponse;
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries &&
                                                  (ex.Message.Contains("SSL connection could not be established") ||
                                                   ex.Message.Contains("remote host closed") ||
                                                   ex.Message.Contains("Connection was closed")))
            {
                retryCount++;
                Console.WriteLine($@"HTTP请求错误 (重试 {retryCount}/{maxRetries}): {ex}");

                // 如果是SSL连接问题，重新初始化HttpClient
                if (ex.Message.Contains("SSL connection could not be established")) ReinitializeHttpClient();

                // 等待一段时间后重试
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount))); // 指数退避
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($@"HTTP请求错误: {ex}");
                if (ex.InnerException != null) Console.WriteLine($@"内部异常: {ex.InnerException.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($@"JSON解析错误: {ex}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($@"请求超时: {ex}");
                throw new Exception("请求超时，请检查网络连接或稍后重试", ex);
            }

        // 如果重试后仍然失败，抛出异常
        throw new HttpRequestException($"在重试{maxRetries}次后仍然无法建立连接");
    }

    /// <summary>
    ///     获取推荐的模组（使用固定源 - 索引0）
    /// </summary>
    public async Task<CurseForgeFeaturedResponse> GetFeaturedModsAsync(int gameId = 78022)
    {
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount <= maxRetries)
            try
            {
                // 构建请求体
                var requestBody = new
                {
                    gameId
                };

                var jsonBody = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // 创建请求 - 使用固定源的 HttpClient
                using var request = new HttpRequestMessage(HttpMethod.Post, "v1/mods/featured")
                {
                    Content = content
                };
                request.Headers.Add("x-api-key", _apiKey);

                var response = await _fixedSourceHttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var featuredResponse = JsonSerializer.Deserialize<CurseForgeFeaturedResponse>(json, options);
                return featuredResponse;
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries &&
                                                  (ex.Message.Contains("SSL connection could not be established") ||
                                                   ex.Message.Contains("remote host closed") ||
                                                   ex.Message.Contains("Connection was closed")))
            {
                retryCount++;
                Console.WriteLine($@"获取推荐内容错误 (重试 {retryCount}/{maxRetries}): {ex}");

                // 如果是SSL连接问题，重新初始化固定源HttpClient
                if (ex.Message.Contains("SSL connection could not be established"))
                    ReinitializeFixedSourceHttpClient();

                // 等待一段时间后重试
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($@"获取推荐内容错误: {ex}");
                if (ex.InnerException != null) Console.WriteLine($@"内部异常: {ex.InnerException.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($@"JSON解析错误: {ex}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($@"请求超时: {ex}");
                throw new Exception("请求超时，请检查网络连接或稍后重试", ex);
            }

        throw new HttpRequestException($"在重试{maxRetries}次后仍然无法建立连接");
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
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount <= maxRetries)
            try
            {
                var url = $"v1/mods/{modId}/files";

                // 创建请求
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", _apiKey);

                var response = await _sharedHttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var filesResponse = JsonSerializer.Deserialize<CurseForgeResponse.ModFilesResponse>(json, options);
                return filesResponse;
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries &&
                                                  (ex.Message.Contains("SSL connection could not be established") ||
                                                   ex.Message.Contains("remote host closed") ||
                                                   ex.Message.Contains("Connection was closed")))
            {
                retryCount++;
                Console.WriteLine($@"获取文件列表错误 (重试 {retryCount}/{maxRetries}): {ex}");

                if (ex.Message.Contains("SSL connection could not be established")) ReinitializeHttpClient();

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($@"获取文件列表错误: {ex}");
                if (ex.InnerException != null) Console.WriteLine($@"内部异常: {ex.InnerException.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($@"JSON解析错误: {ex}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($@"请求超时: {ex}");
                throw new Exception("请求超时，请检查网络连接或稍后重试", ex);
            }

        throw new HttpRequestException($"在重试{maxRetries}次后仍然无法建立连接");
    }

    /// <summary>
    ///     获取指定文件ID的详细信息
    /// </summary>
    public async Task<CurseForgeResponse.ModFile> GetFileDetailsAsync(int fileId)
    {
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount <= maxRetries)
            try
            {
                var url = $"v1/mods/files/{fileId}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", _apiKey);

                var response = await _sharedHttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var fileResponse = JsonSerializer.Deserialize<CurseForgeResponse.SingleFileResponse>(json, options);
                return fileResponse?.Data;
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries &&
                                                  (ex.Message.Contains("SSL connection could not be established") ||
                                                   ex.Message.Contains("remote host closed") ||
                                                   ex.Message.Contains("Connection was closed")))
            {
                retryCount++;
                Console.WriteLine($@"获取文件详情错误 (重试 {retryCount}/{maxRetries}): {ex}");

                if (ex.Message.Contains("SSL connection could not be established")) ReinitializeHttpClient();

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($@"获取文件详情错误: {ex}");
                if (ex.InnerException != null) Console.WriteLine($@"内部异常: {ex.InnerException.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($@"JSON解析错误: {ex}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($@"请求超时: {ex}");
                throw new Exception("请求超时，请检查网络连接或稍后重试", ex);
            }

        throw new HttpRequestException($"在重试{maxRetries}次后仍然无法建立连接");
    }

    /// <summary>
    ///     获取多个文件的详细信息
    /// </summary>
    public async Task<List<CurseForgeResponse.ModFile>> GetMultipleFilesAsync(int[] fileIds)
    {
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount <= maxRetries)
            try
            {
                var requestBody = new
                {
                    fileIds
                };

                var jsonBody = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, "v1/mods/files")
                {
                    Content = content
                };
                request.Headers.Add("x-api-key", _apiKey);

                var response = await _sharedHttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var filesResponse = JsonSerializer.Deserialize<CurseForgeResponse.ModFilesResponse>(json, options);
                return filesResponse?.Data ?? new List<CurseForgeResponse.ModFile>();
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries &&
                                                  (ex.Message.Contains("SSL connection could not be established") ||
                                                   ex.Message.Contains("remote host closed") ||
                                                   ex.Message.Contains("Connection was closed")))
            {
                retryCount++;
                Console.WriteLine($@"获取多个文件错误 (重试 {retryCount}/{maxRetries}): {ex}");

                if (ex.Message.Contains("SSL connection could not be established")) ReinitializeHttpClient();

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($@"获取多个文件错误: {ex}");
                if (ex.InnerException != null) Console.WriteLine($@"内部异常: {ex.InnerException.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($@"JSON解析错误: {ex}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($@"请求超时: {ex}");
                throw new Exception("请求超时，请检查网络连接或稍后重试", ex);
            }

        throw new HttpRequestException($"在重试{maxRetries}次后仍然无法建立连接");
    }

    /// <summary>
    ///     获取指定 modId 的 Markdown 格式描述（使用固定源 - 索引0）
    /// </summary>
    /// <param name="modId">模组ID</param>
    /// <returns>模组的 Markdown 描述内容</returns>
    public async Task<string> GetModDescriptionAsync(int modId)
    {
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount <= maxRetries)
            try
            {
                var url = $"v1/mods/{modId}/description";

                // 创建请求 - 使用固定源的 HttpClient
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", _apiKey);

                var response = await _fixedSourceHttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var descriptionResponse =
                    JsonSerializer.Deserialize<CurseForgeResponse.ModDescriptionResponse>(json, options);
                return descriptionResponse?.Data ?? string.Empty;
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries &&
                                                  (ex.Message.Contains("SSL connection could not be established") ||
                                                   ex.Message.Contains("remote host closed") ||
                                                   ex.Message.Contains("Connection was closed")))
            {
                retryCount++;
                Console.WriteLine($@"获取模组描述错误 (重试 {retryCount}/{maxRetries}): {ex}");

                // 如果是SSL连接问题，重新初始化固定源HttpClient
                if (ex.Message.Contains("SSL connection could not be established"))
                    ReinitializeFixedSourceHttpClient();

                // 等待一段时间后重试
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($@"获取模组描述错误: {ex}");
                if (ex.InnerException != null)
                    Console.WriteLine($@"内部异常: {ex.InnerException.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($@"JSON解析错误: {ex}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($@"请求超时: {ex}");
                throw new Exception("请求超时，请检查网络连接或稍后重试", ex);
            }

        throw new HttpRequestException($"在重试{maxRetries}次后仍然无法建立连接");
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