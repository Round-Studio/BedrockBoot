using BedrockBoot.Models.Entry.Update;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Classes.Update;

public class Update
{
    public string Url { get; private set; }
    public Action<string, string,string , string> OnUpdate { get; set; }
    public Action OnUnUpdate { get; set; }
    public Update(
        string updateUrl = "https://api.github.com/repos/Round-Studio/BedrockBoot/releases/latest")
    {
        Url = updateUrl;
    }
    private async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        using HttpClient httpClient = new HttpClient();
        string url = Url;

        // 添加必要的请求头
        httpClient.DefaultRequestHeaders.Add("User-Agent", $"BedrockBoot/{Assembly.GetEntryAssembly()?.GetName().Version.ToString()}");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var release = JsonSerializer.Deserialize<GitHubRelease>(jsonResponse, options);

            return release;
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

    public async Task TryCheckUdate()
    {
        var nowVersion = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
        var githubRelease = await GetLatestReleaseAsync();

        if (nowVersion
                .Replace("0", "")
                .Replace(".", "") !=
            githubRelease.Name
                .Replace("0", "")
                .Replace(".", "")
                .Replace("v", ""))
        {
            OnUpdate.Invoke(nowVersion, githubRelease.Name, githubRelease.Body, githubRelease.Assets[0].BrowserDownloadUrl);
        }
        else
        {
            OnUnUpdate.Invoke();
        }
    }
}