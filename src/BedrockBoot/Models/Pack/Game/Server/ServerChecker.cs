using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game.Pack.Server;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Pack.Game.Server;

public class ServerChecker
{
    private readonly HttpClient _httpClient;
    
    public ServerChecker()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        
        // 设置请求头
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MinecraftServerChecker/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }
    
    // 异步获取服务器状态
    public async Task<ServerStatusResponse> GetServerStatusAsync(ServerItemInfo info)
    {
        try
        {
            string apiUrl = SourceList.ServerStatusApi.Replace("{ip}", info.ServerAddress)
                .Replace("{port}", info.ServerPort.ToString());
            
            Console.WriteLine($"请求URL: {apiUrl}");
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();
            string jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"响应JSON: {jsonResponse}");
            
            var serverStatus = JsonSerializer.Deserialize<ServerStatusResponse>(jsonResponse);
            
            return serverStatus;
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
        catch (Exception ex)
        {
            Console.WriteLine($"其他错误: {ex.Message}");
            throw;
        }
    }
}