using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Interface;

namespace BedrockBoot.Models.Translate;

public class MicrosoftTranslateService : ITranslationService
{
    private static readonly HttpClient _httpClient = new();
    private static string _token;
    private static DateTime _tokenExpiryTime = DateTime.MinValue;

    // 简化语言代码映射表 - 不使用复杂的比较器
    private static readonly Dictionary<string, string> _languageCodeMap = new()
    {
        // 中文变体
        { "zh_cn", "zh-Hans" },
        { "zh-tw", "zh-Hant" },
        { "zh_hk", "zh-Hant" },
        { "zh_tw", "zh-Hant" },
        { "zh-cn", "zh-Hans" },
        { "zh-hk", "zh-Hant" },
        { "zh_sg", "zh-Hans" },
        { "zh", "zh-Hans" },

        // 其他常见语言
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

    // 静态构造器 - 可以用来捕获初始化异常
    static MicrosoftTranslateService()
    {
        try
        {
            // 可以在这里添加一些初始化代码
            Console.WriteLine(@"MicrosoftTranslateService 静态初始化成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"MicrosoftTranslateService 静态初始化失败: {ex.Message}");
            throw; // 重新抛出，但我们可以记录日志
        }
    }

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        try
        {
            // 确保令牌有效
            await EnsureValidTokenAsync();

            var requestBody = BuildRequestBody(new List<string> { text });
            var jsonResponse = await CallTranslationApiAsync(requestBody, sourceLanguage, targetLanguage);

            // 解析单个结果
            var result = GetTextFromJson(jsonResponse);
            return result ?? text; // 如果翻译失败，返回原文
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($@"HTTP错误 (状态码: {httpEx.StatusCode}): {httpEx.Message}");
            Console.WriteLine($@"请求的文本: {text}");
            return text; // HTTP错误时返回原文
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"翻译失败: {ex.Message}");
            Console.WriteLine($@"请求的文本: {text}");
            return text; // 发生异常时返回原文
        }
    }

    private async Task EnsureValidTokenAsync()
    {
        // 检查令牌是否为空或即将过期（提前5分钟刷新）
        if (string.IsNullOrEmpty(_token) || DateTime.UtcNow >= _tokenExpiryTime.AddMinutes(-5))
        {
            _token = await GetAPITokenAsync();
            if (!string.IsNullOrEmpty(_token))
            {
                // 假设令牌有效期为10分钟，设置为9分钟，留出1分钟缓冲
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

        // 转为小写以便匹配字典
        var lowerCode = languageCode.ToLowerInvariant();

        // 如果语言代码已经是标准格式，直接返回
        if (lowerCode.Contains("-"))
        {
            // 确保中文代码正确
            if (lowerCode.StartsWith("zh")) return lowerCode == "zh-hant" ? "zh-Hant" : "zh-Hans";
            return languageCode; // 保持原格式（可能包含大小写）
        }

        // 查找映射
        if (_languageCodeMap.TryGetValue(lowerCode, out var mappedCode)) return mappedCode;

        // 特殊处理中文
        if (lowerCode.StartsWith("zh")) return "zh-Hans";

        // 如果没有映射，返回原代码（但记录警告）
        Console.WriteLine($@"警告: 未找到语言代码映射: {languageCode}，使用原代码");
        return languageCode;
    }

    private async Task<string> CallTranslationApiAsync(string requestBody, string sourceLanguage, string targetLanguage)
    {
        // 确保令牌有效
        await EnsureValidTokenAsync();

        // 转换语言代码
        var convertedTargetLanguage = ConvertLanguageCode(targetLanguage);
        string convertedSourceLanguage = null;

        if (!string.IsNullOrEmpty(sourceLanguage) && !sourceLanguage.Equals("auto", StringComparison.OrdinalIgnoreCase))
            convertedSourceLanguage = ConvertLanguageCode(sourceLanguage);

        // 构建请求URL
        var url =
            $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to={convertedTargetLanguage}&textType=plain";

        // 添加源语言参数
        if (!string.IsNullOrEmpty(convertedSourceLanguage)) url += $"&from={convertedSourceLanguage}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        // 清除可能存在的旧请求头
        request.Headers.Clear();

        // 按照正确顺序添加请求头
        request.Headers.UserAgent.ParseAdd("RoundSmartTerminals/ver2 (https://round-studio.github.io)");
        request.Headers.Add("Authorization", "Bearer " + _token);
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("Cache-Control", "no-cache");
        request.Headers.Add("Host", "api.cognitive.microsofttranslator.com");
        request.Headers.Add("Connection", "keep-alive");

        try
        {
            var response = await _httpClient.SendAsync(request);

            // 添加错误诊断
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($@"翻译API错误 - 状态码: {response.StatusCode}");
                Console.WriteLine($@"请求URL: {url}");
                Console.WriteLine($@"请求体: {requestBody}");
                Console.WriteLine($@"错误响应: {errorContent}");

                // 尝试提取更详细的错误信息
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
                    // 忽略JSON解析错误
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
            // 使用System.Text.Json进行正确的JSON转义
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
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "RoundSmartTerminals/ver2 (https://round-studio.github.io)");

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

    private static List<string> ParseTranslations(string json)
    {
        var results = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.EnumerateArray())
                if (item.TryGetProperty("translations", out var translations) &&
                    translations.GetArrayLength() > 0)
                {
                    var translation = translations[0];
                    if (translation.TryGetProperty("text", out var text))
                        results.Add(text.GetString() ?? string.Empty);
                    else
                        results.Add(string.Empty);
                }
                else
                {
                    results.Add(string.Empty);
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"解析批量翻译响应失败: {ex.Message}, 响应: {json}");
        }

        return results;
    }
}