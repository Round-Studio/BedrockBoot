using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;

public class CurseForgeApiClient
{
    private static readonly HttpClient _sharedHttpClient;
    private readonly string _apiKey;

    // 静态构造函数，只初始化一次 HttpClient
    static CurseForgeApiClient()
    {
        _sharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30),
            BaseAddress = new Uri("https://api.curseforge.com/"),
            // 强制使用 HTTP/1.1 避免 HTTP/2 问题
            DefaultRequestVersion = HttpVersion.Version11
        };
        
        // 设置默认请求头
        _sharedHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"BedrockBoot/{GlobalModel.BodyVersion}");
        _sharedHttpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public CurseForgeApiClient(string apiKey)
    {
        _apiKey = apiKey;
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
        try
        {
            // 构建参数
            var queryParams = new System.Text.StringBuilder();
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
            
            string url = $"v1/mods/search{queryParams}";
            
            // 创建请求
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", _apiKey);
            
            var response = await _sharedHttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            string json = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var curseForgeResponse = JsonSerializer.Deserialize<CurseForgeResponse>(json, options);
            return curseForgeResponse;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP请求错误: {ex.Message}");
            throw;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON解析错误: {ex.Message}");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"请求超时: {ex.Message}");
            throw new Exception("请求超时，请检查网络连接或稍后重试", ex);
        }
    }
    
    // 新增：获取推荐内容方法（类似参考代码的 GetFeatured）
    public async Task<CurseForgeResponse> GetFeaturedModsAsync(int gameId = 78022)
    {
        try
        {
            // 构建请求体
            var requestBody = new
            {
                gameId
            };
            
            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
            
            // 创建请求
            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/mods/featured")
            {
                Content = content
            };
            request.Headers.Add("x-api-key", _apiKey);
            
            var response = await _sharedHttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            string json = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var featuredResponse = JsonSerializer.Deserialize<CurseForgeResponse>(json, options);
            return featuredResponse;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"获取推荐内容错误: {ex.Message}");
            throw;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON解析错误: {ex.Message}");
            throw;
        }
    }
}