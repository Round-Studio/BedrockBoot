using System.Net.Http;
using System.Text.Json;
using BedrockBoot.Downloader.Global;
using BedrockBoot.Downloader.Manifest.Game;

namespace BedrockBoot.Downloader.Game;

partial class ModernVersionHelper
{
    private static readonly HttpClient httpClient = new HttpClient();
    public static string BaseUrl => $"{SourceList.BaseUrl}/versions";

    static ModernVersionHelper()
    {
        httpClient.DefaultRequestHeaders.Add("User-Agent", GameDownloader.UserAgent);
    }

    public static ModernManifest GetManifest()
    {
        var response = httpClient.GetStringAsync($"{BaseUrl}").Result;
        var manifest = JsonSerializer.Deserialize<ModernManifest>(response);
        return manifest;
    }
}