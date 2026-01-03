using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game.Pack.Mods.CurseForge;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Pack.Game.Mods.CurseForge;

public class CurseForgeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public CurseForgeApiClient(string apiKey)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
        
        // 设置默认请求头
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"Round-Studio (BedrockBoot)/{GlobalModel.BodyVersion}");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<CurseForgeResponse> SearchModsAsync(
        int gameId = 78022, 
        string searchFilter = "", 
        int pageSize = 20)
    {
        try
        {
            string url = $"https://api.curseforge.com/v1/mods/search?gameId={gameId}&searchFilter={Uri.EscapeDataString(searchFilter)}&pageSize={pageSize}";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            string json = await response.Content.ReadAsStringAsync();
            
            // 使用 System.Text.Json 反序列化
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
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
    }
}