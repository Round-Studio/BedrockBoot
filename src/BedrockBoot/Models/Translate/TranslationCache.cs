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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Translate;

/// <summary>
///     翻译结果的本地缓存：JSON 字典，键为 "文本哈希_目标语言"。
///     存放在启动器临时目录，同一段描述第二次查看/翻译时直接命中，不再请求接口。
/// </summary>
public static class TranslationCache
{
    private static readonly string CacheFilePath =
        Path.Combine(PathsList.TempPath, "translation_cache.json");

    private static readonly object _lock = new();
    private static Dictionary<string, string>? _cache;

    /// <summary>批量查询缓存，返回与入参等长的数组，未命中的槽位为 null。</summary>
    public static string?[] GetOrVisit(IReadOnlyList<string> texts, string targetLanguage)
    {
        var hits = new string?[texts.Count];
        var cache = Load();
        lock (_lock)
        {
            for (var i = 0; i < texts.Count; i++)
                cache.TryGetValue(BuildKey(texts[i], targetLanguage), out hits[i]);
        }

        return hits;
    }

    /// <summary>批量写入并落盘。写入失败不影响使用（缓存只是加速）。</summary>
    public static void Put(IReadOnlyDictionary<string, string> entries)
    {
        if (entries.Count == 0) return;

        try
        {
            lock (_lock)
            {
                var cache = Load();
                foreach (var (key, value) in entries)
                    cache[key] = value;

                Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);
                File.WriteAllText(CacheFilePath, JsonSerializer.Serialize(cache));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"写入翻译缓存失败: {ex.Message}");
        }
    }

    private static Dictionary<string, string> Load()
    {
        // 调用方必须已持有 _lock；保持单入口便于维护
        if (_cache != null) return _cache;

        _cache = new Dictionary<string, string>();
        try
        {
            if (File.Exists(CacheFilePath))
            {
                var json = File.ReadAllText(CacheFilePath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (loaded != null) _cache = loaded;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取翻译缓存失败: {ex.Message}");
        }

        return _cache;
    }

    private static string BuildKey(string text, string targetLanguage)
    {
        // SHA1 足够区分描述片段且比长文本本身短得多，原文不落明文
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(text));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) builder.Append(b.ToString("x2"));
        return $"{builder}_{targetLanguage.ToLowerInvariant()}";
    }
}
