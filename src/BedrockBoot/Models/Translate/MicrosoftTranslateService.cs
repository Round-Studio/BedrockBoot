/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Standard.Interface;

namespace BedrockBoot.Models.Translate;

public class MicrosoftTranslateService : ITranslationService
{
    private static readonly HttpClient _httpClient = new();

    // 2026-09 起 edge.microsoft.com/translate/auth 令牌接口已下线（404），
    // 现走 Edge 浏览器翻译的新端点，免鉴权，响应结构与原认知服务 API 相同
    private const string TranslateApiUrl = "https://edge.microsoft.com/translate/translatetext";

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
        { "yue_hk", "yue" },

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

    /// <summary>
    /// 翻译 HTML 片段，标签结构原样保留（接口 textType=html）。
    /// 单段超长时按标签边界分块请求，失败时原样返回。
    /// </summary>
    public async Task<string> TranslateHtmlAsync(string html, string sourceLanguage, string targetLanguage)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        try
        {
            var builder = new StringBuilder();
            foreach (var chunk in SplitHtml(html, 12000))
            {
                var requestBody = BuildRequestBody(new List<string> { chunk });
                var jsonResponse =
                    await CallTranslationApiAsync(requestBody, sourceLanguage, targetLanguage, "html");

                builder.Append(GetTextFromJson(jsonResponse) ?? chunk);
            }

            return builder.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"HTML 翻译失败: {ex.Message}");
            return html;
        }
    }

    private static List<string> SplitHtml(string html, int maxLength)
    {
        var chunks = new List<string>();
        if (html.Length <= maxLength)
        {
            chunks.Add(html);
            return chunks;
        }

        var start = 0;
        while (start < html.Length)
        {
            var end = Math.Min(start + maxLength, html.Length);
            if (end < html.Length)
            {
                // 优先在相邻标签的 "><" 处切分，避免截断标签内部
                var boundary = html.LastIndexOf("><", end - 1, end - start, StringComparison.Ordinal);
                if (boundary > start) end = boundary + 1;
            }

            chunks.Add(html[start..end]);
            start = end;
        }

        return chunks;
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

    private async Task<string> CallTranslationApiAsync(string requestBody, string sourceLanguage,
        string targetLanguage, string textType = "plain")
    {
        var convertedTargetLanguage = ConvertLanguageCode(targetLanguage);
        string convertedSourceLanguage = null;

        if (!string.IsNullOrEmpty(sourceLanguage) && !sourceLanguage.Equals("auto", StringComparison.OrdinalIgnoreCase))
            convertedSourceLanguage = ConvertLanguageCode(sourceLanguage);

        var from = convertedSourceLanguage ?? "";
        var url =
            $"{TranslateApiUrl}?from={from}&to={convertedTargetLanguage}&textType={textType}&isEnterpriseClient=false";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        request.Headers.Clear();
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        request.Headers.Add("Accept", "*/*");

        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($@"翻译API错误 - 状态码: {response.StatusCode}");
                Console.WriteLine($@"请求URL: {url}");
                Console.WriteLine($@"错误响应: {errorContent}");

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
        // translatetext 端点接收纯字符串数组，而非旧 API 的 [{"Text":...}] 对象数组
        var entries = new List<string>();
        foreach (var text in texts)
            entries.Add(JsonSerializer.Serialize(text));

        return "[" + string.Join(",", entries) + "]";
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
