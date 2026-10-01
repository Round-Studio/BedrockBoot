using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Pack.Xbox.Cape;

public static class CapeApiClient
{
    public static string UserAgent { get; set; } = $"BedrockBoot/{GlobalModel.BodyVersion}";

    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task<CapeResponse?> GetPlayerCapesAsync(string xboxToken)
    {
        string url = "https://be-cape.roundstudio.top/api/v1/getPlayerCapes";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        request.Content = JsonContent.Create(new { xboxToken });

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<CapeResponse>(json, options);
    }
}