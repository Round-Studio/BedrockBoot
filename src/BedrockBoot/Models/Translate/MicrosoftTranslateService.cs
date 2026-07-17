using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Translate;

public class MicrosoftTranslateService : ITranslationService
{
    private static readonly HttpClient _httpClient = new();
    private static string _token;
    private static DateTime _tokenExpiryTime = DateTime.MinValue;

    private static readonly Dictionary<string, string> _languageCodeMap = new()
    {
        { "zh_cn", "zh-Hans" },
        { "zh-tw", "zh-Hant" },
        { "zh_hk", "zh-Hant" },
        { "zh_tw", "zh-Hant" },
        { "zh-cn", "zh-Hans" },
        { "zh-hk", "zh-Hant" },
        { "zh_sg", "zh-Hans" },
        { "zh", "zh-Hans" },

        { "en_us", "en" },
        { "en_gb", "en" },
        { "en", "en" },
        { "ja_jp", "ja" },
        { "ja", "ja" },
        { "ko_kr", "ko" },
        { "ko", "ko" },
        { "fr_fr", "fr" },
        { "fr", "fr" },
        { "de_de", "de" },
        { "de", "de" },
        { "es_es", "es" },
        { "es", "es" },
        { "ru_ru", "ru" },
        { "ru", "ru" }
    };

    static MicrosoftTranslateService()
    {
        try
        {
            Console.WriteLine(@"MicrosoftTranslateService 静态初始化成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"MicrosoftTranslateService 静态初始化失败: {ex.Message}");
            throw;
        }
    }

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        try
        {
            await EnsureValidTokenAsync();

            var requestBody = BuildRequestBody(new List<string> { text });
            var jsonResponse = await CallTranslationApiAsync(requestBody, sourceLanguage, targetLanguage);

            var result = GetTextFromJson(jsonResponse);
            return result ?? text;
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($@"HTTP错误 (状态码: {httpEx.StatusCode}): {httpEx.Message}");
            Console.WriteLine($@"请求的文本: {text}");
            return text;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"翻译失败: {ex.Message}");
            Console.WriteLine($@"请求的文本: {text}");
            return text;
        }
    }

    private async Task EnsureValidTokenAsync()
    {
        if (string.IsNullOrEmpty(_token) || DateTime.UtcNow >= _tokenExpiryTime.AddMinutes(-5))
        {
            _token = await GetAPITokenAsync();
            if (!string.IsNullOrEmpty(_token))
            {
                _tokenExpiryTime = DateTime.UtcNow.AddMinutes(9);
                Console.WriteLine(@"Token刷新成功");
            }
            else
            {
                Console.WriteLine(@"警告: 获取Token失败");
            }
        }
    }

    private string ConvertLanguageCode(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
            return languageCode;

        var lowerCode = languageCode.ToLowerInvariant();

        if (lowerCode.Contains("-"))
        {
            if (lowerCode.StartsWith("zh")) return lowerCode == "zh-hant" ? "zh-Hant" : "zh-Hans";
            return languageCode;
        }

        if (_languageCodeMap.TryGetValue(lowerCode, out var mappedCode)) return mappedCode;

        if (lowerCode.StartsWith("zh")) return "zh-Hans";

        Console.WriteLine($@"警告: 未找到语言代码映射: {languageCode}，使用原代码");
        return languageCode;
    }

    private async Task<string> CallTranslationApiAsync(string requestBody, string sourceLanguage, string targetLanguage)
    {
        await EnsureValidTokenAsync();

        var convertedTargetLanguage = ConvertLanguageCode(targetLanguage);
        string convertedSourceLanguage = null;

        if (!string.IsNullOrEmpty(sourceLanguage) && !sourceLanguage.Equals("auto", StringComparison.OrdinalIgnoreCase))
            convertedSourceLanguage = ConvertLanguageCode(sourceLanguage);

        var url =
            $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to={convertedTargetLanguage}&textType=plain";

        if (!string.IsNullOrEmpty(convertedSourceLanguage)) url += $"&from={convertedSourceLanguage}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        request.Headers.Clear();

        request.Headers.UserAgent.ParseAdd("RoundSmartTerminals/ver2 (https://round-studio.github.io)");
        request.Headers.Add("Authorization", "Bearer " + _token);
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("Cache-Control", "no-cache");
        request.Headers.Add("Host", "api.cognitive.microsofttranslator.com");
        request.Headers.Add("Connection", "keep-alive");

        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($@"翻译API错误 - 状态码: {response.StatusCode}");
                Console.WriteLine($@"请求URL: {url}");
                Console.WriteLine($@"请求体: {requestBody}");
                Console.WriteLine($@"错误响应: {errorContent}");

                try
                {
                    using var errorDoc = JsonDocument.Parse(errorContent);
                    if (errorDoc.RootElement.TryGetProperty("error", out var error))
                    {
                        if (error.TryGetProperty("message", out var message))
                            Console.WriteLine($@"错误消息: {message.GetString()}");
                        if (error.TryGetProperty("code", out var code)) Console.WriteLine($@"错误代码: {code.GetString()}");
                    }
                }
                catch
                {
                }

                response.EnsureSuccessStatusCode();
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($@"HTTP请求异常: {ex.Message}");
            throw;
        }
    }

    private string BuildRequestBody(List<string> texts)
    {
        var entries = new List<string>();
        foreach (var text in texts)
        {
            var escapedText = JsonSerializer.Serialize(text);
            entries.Add($"{{\"Text\":{escapedText}}}");
        }

        return "[" + string.Join(",", entries) + "]";
    }

    private static async Task<string> GetAPITokenAsync()
    {
        using var client = new HttpClient();
        try
        {
            client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.ConnectionClose = false;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(GlobalModel.BodyVersion);

            var response = await client.GetAsync("https://edge.microsoft.com/translate/auth");
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadAsStringAsync();
            return token;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"获取API令牌失败: {ex.Message}");
            return null;
        }
    }

    private static string GetTextFromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.EnumerateArray())
                if (item.TryGetProperty("translations", out var translations) &&
                    translations.GetArrayLength() > 0)
                {
                    var translation = translations[0];
                    if (translation.TryGetProperty("text", out var text)) return text.GetString();
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"解析翻译响应失败: {ex.Message}, 响应: {json}");
        }

        return null;
    }
}