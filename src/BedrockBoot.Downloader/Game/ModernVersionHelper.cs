using System.Net.Http;
using System.Text.Json;
using BedrockBoot.Downloader.Global;
using BedrockBoot.Downloader.Manifest.Game;

namespace BedrockBoot.Downloader.Game
{
    public static class ModernVersionHelper
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public static string BaseUrl => $"{SourceList.BaseUrl}/versions";

        static ModernVersionHelper()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", GameDownloader.UserAgent);
        }

        public static ModernManifest GetManifest()
        {
            var response = httpClient.GetStringAsync($"{BaseUrl}/manifest.json").Result;
            var manifest = JsonSerializer.Deserialize<ModernManifest>(response);
            return manifest;
        }
    }
}