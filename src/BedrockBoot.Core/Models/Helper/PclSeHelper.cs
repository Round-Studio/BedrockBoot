/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BedrockBoot.Standard.Entity;
using BedrockBoot.Standard.Enum.Type;

namespace BedrockBoot.Core.Models.Helper;

/// <summary>
/// PCLSE 目录导入的辅助逻辑：受限的 YAML 读取、路径校验与版本布局探测。
/// 与界面无关，便于独立测试。
/// </summary>
public static class PclSeHelper
{
    // 配置文件与解析结果都设上限：PCLSE 配置由外部程序写入，不能假定其体积和条目数量可控。
    private const long MaxConfigFileBytes = 512 * 1024;
    private const int MaxConfigLines = 4096;
    private const int MaxConfigLineLength = 2048;
    private const int MaxConfigEntries = 256;
    private const int MaxConfigValueLength = 1024;
    private const int MaxDirectoryScan = 256;
    private const int MaxReparsePointHops = 32;

    private static readonly string[] ConfigKeys = { "BedrockFolder", "BedrockExternalFolders" };

    public static bool IsPclSeRegistration(GameFolderInfo? folder) =>
        folder != null &&
        (folder.GameFolderType == GameFolderType.PCLSE ||
         string.Equals(folder.GameFolderName, "PCLSE", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 从启动器目录逐级向上查找包含 PCL\config.v1.yml 的目录。
    /// </summary>
    public static IEnumerable<string> GetRoots(string launcherRoot)
    {
        if (!TryNormalizeLocalPath(launcherRoot, null, out var current)) yield break;

        for (var i = 0; i < 5 && !string.IsNullOrWhiteSpace(current); i++)
        {
            if (LocalFileExists(Path.Combine(current, "PCL", "config.v1.yml"))) yield return current;

            string? parent;
            try
            {
                parent = Directory.GetParent(current)?.FullName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查找 PCLSE 上级目录失败（{current}）：{ex.Message}");
                yield break;
            }

            if (string.IsNullOrEmpty(parent) ||
                string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
                break;

            current = parent;
        }
    }

    /// <summary>
    /// 把 PCLSE 配置里的目录适配成启动器使用的数据根目录。
    /// </summary>
    public static string? PrepareFolder(string source)
    {
        try
        {
            if (!TryNormalizeLocalPath(source, null, out var normalized) || !Directory.Exists(normalized))
                return null;

            var leafName = Path.GetFileName(normalized);
            if (leafName.Equals("versions", StringComparison.OrdinalIgnoreCase) ||
                leafName.Equals("bedrock_versions", StringComparison.OrdinalIgnoreCase))
                return Path.GetDirectoryName(normalized);

            if (LocalDirectoryExists(Path.Combine(normalized, "versions")) ||
                LocalDirectoryExists(Path.Combine(normalized, "bedrock_versions")))
                return normalized;

            if (leafName.Equals("extracted", StringComparison.OrdinalIgnoreCase) &&
                Path.GetDirectoryName(normalized) is { } versionsDirectory &&
                (Path.GetFileName(versionsDirectory).Equals("versions", StringComparison.OrdinalIgnoreCase) ||
                 Path.GetFileName(versionsDirectory).Equals("bedrock_versions", StringComparison.OrdinalIgnoreCase)))
                return Path.GetDirectoryName(versionsDirectory);

            // 配置可能直接指向某个版本目录（例如 ...\extracted\<版本>）。
            // 这时要注册它所属的数据根目录，否则会把版本目录当成数据根、扫描不到任何实例。
            if (LooksLikeGameInstance(normalized))
            {
                var parent = Path.GetDirectoryName(normalized);
                if (parent != null &&
                    Path.GetFileName(parent).Equals("extracted", StringComparison.OrdinalIgnoreCase))
                    parent = Path.GetDirectoryName(parent);

                if (parent != null &&
                    (Path.GetFileName(parent).Equals("versions", StringComparison.OrdinalIgnoreCase) ||
                     Path.GetFileName(parent).Equals("bedrock_versions", StringComparison.OrdinalIgnoreCase)))
                    return Path.GetDirectoryName(parent);

                return normalized;
            }

            return HasVersionDirectories(normalized) ? normalized : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"适配 PCLSE 目录失败（{source}）：{ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 判断已注册目录是否真的包含 PCLSE 的版本布局或可发现的游戏实例。
    /// 只要目录存在就认为导入成功会把空目录也算进来，因此必须额外校验内容。
    /// </summary>
    public static bool IsUsableFolder(string folder)
    {
        try
        {
            if (!TryNormalizeLocalPath(folder, null, out var normalized) || !Directory.Exists(normalized))
                return false;

            if (LooksLikeGameInstance(normalized)) return true;

            return ContainsGameInstance(normalized) || HasVersionLayout(normalized);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检查 PCLSE 目录失败（{folder}）：{ex.Message}");
            return false;
        }
    }

    public static List<string> ReadDirectories(string launcherRoot)
    {
        var result = new List<string>();
        if (!TryNormalizeLocalPath(Path.Combine(launcherRoot, "PCL", "config.v1.yml"), null, out var configPath) ||
            !File.Exists(configPath))
            return result;

        var values = ReadConfig(configPath);

        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!TryNormalizeLocalPath(path, launcherRoot, out var fullPath))
            {
                Console.WriteLine($"忽略远程或无效的 PCLSE 基岩目录：{path}");
                return;
            }

            if (!result.Any(existing => PathsEqual(existing, fullPath))) result.Add(fullPath);
        }

        if (values.TryGetValue("BedrockFolder", out var folders) && folders.Count > 0)
            Add(folders[0]);
        else
        {
            // PCLSE 现在默认使用 .minecraft\\bedrock，旧版本则使用
            // .minecraft\\bedrock_versions 或启动器根目录下的 bedrock_versions。
            Add(Path.Combine(launcherRoot, ".minecraft", "bedrock"));
            Add(Path.Combine(launcherRoot, ".minecraft", "bedrock_versions"));
            Add(Path.Combine(launcherRoot, "bedrock_versions"));
        }

        // BedrockExternalFolders 既可能写成 YAML 序列，也可能写成以分号分隔的标量。
        if (values.TryGetValue("BedrockExternalFolders", out var externalFolders))
            foreach (var externalValue in externalFolders)
                foreach (var external in externalValue.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    Add(external.Trim());

        return result;
    }

    /// <summary>
    /// 只解析 PCLSE 的基岩目录相关键，并限制读取的行数、条目数与单个值的长度。
    /// </summary>
    private static Dictionary<string, List<string>> ReadConfig(string path)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in ConfigKeys) result[key] = new List<string>();

        string? sequenceKey = null;
        foreach (var line in ReadBoundedLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

            // YAML 序列项归属到上一个取值为空的键（例如 BedrockExternalFolders）。
            if (trimmed == "-") continue;
            if (trimmed.Length > 1 && trimmed[0] == '-' && char.IsWhiteSpace(trimmed[1]))
            {
                if (sequenceKey != null)
                    AddBoundedValue(result[sequenceKey], ParseValue(StripComment(trimmed[1..].Trim())));
                continue;
            }

            var separator = trimmed.IndexOf(':');
            if (separator <= 0)
            {
                sequenceKey = null;
                continue;
            }

            var key = trimmed[..separator].Trim();
            if (!result.TryGetValue(key, out var values))
            {
                // 不需要的键直接跳过，避免把整份配置读进内存。
                sequenceKey = null;
                continue;
            }

            var value = StripComment(trimmed[(separator + 1)..].Trim());
            sequenceKey = value.Length == 0 ? key : null;

            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                foreach (var item in SplitFlowSequence(value[1..^1]))
                    AddBoundedValue(values, ParseValue(item));
            }
            else if (value.Length > 0)
            {
                AddBoundedValue(values, ParseValue(value));
            }
        }

        return result;
    }

    private static IEnumerable<string> ReadBoundedLines(string path)
    {
        long length;
        try
        {
            length = new FileInfo(path).Length;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取 PCLSE 配置失败（{path}）：{ex.Message}");
            yield break;
        }

        if (length > MaxConfigFileBytes)
        {
            Console.WriteLine($"忽略过大的 PCLSE 配置文件（{length} 字节）：{path}");
            yield break;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);

        for (var i = 0; i < MaxConfigLines; i++)
        {
            var line = reader.ReadLine();
            if (line == null) yield break;
            if (line.Length > MaxConfigLineLength) continue;
            yield return line;
        }
    }

    private static void AddBoundedValue(List<string> values, string value)
    {
        if (values.Count >= MaxConfigEntries) return;

        var trimmed = value.Trim();
        if (trimmed.Length == 0) return;
        if (trimmed.Length > MaxConfigValueLength) trimmed = trimmed[..MaxConfigValueLength];
        values.Add(trimmed);
    }

    private static string ParseValue(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value[1..^1].Replace("\\\\", "\\").Replace("\\\"", "\"");
        if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
            return value[1..^1].Replace("''", "'");
        return value;
    }

    private static string StripComment(string value)
    {
        if (value.Length == 0) return value;

        char quote = '\0';
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (quote != '\0')
            {
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                quote = ch;
                continue;
            }

            if (ch == '#' && (i == 0 || char.IsWhiteSpace(value[i - 1])))
                return value[..i].TrimEnd();
        }

        return value;
    }

    private static IEnumerable<string> SplitFlowSequence(string value)
    {
        var items = new List<string>();
        if (value.Length == 0) return items;

        var buffer = new StringBuilder();
        char quote = '\0';
        foreach (var ch in value)
        {
            if (quote != '\0')
            {
                buffer.Append(ch);
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                quote = ch;
                buffer.Append(ch);
                continue;
            }

            if (ch == ',')
            {
                if (buffer.Length > 0) items.Add(buffer.ToString().Trim());
                buffer.Clear();
                continue;
            }

            buffer.Append(ch);
        }

        if (buffer.Length > 0) items.Add(buffer.ToString().Trim());
        return items;
    }

    private static bool ContainsGameInstance(string folder, int depth = 0)
    {
        if (depth > 2) return false;

        foreach (var directory in EnumerateLocalDirectories(folder))
        {
            var name = Path.GetFileName(directory);
            if (name.Equals("cache", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("loaders", StringComparison.OrdinalIgnoreCase))
                continue;

            if (name.Equals("extracted", StringComparison.OrdinalIgnoreCase))
            {
                if (ContainsGameInstance(directory, depth + 1)) return true;
                continue;
            }

            if (LooksLikeGameInstance(directory)) return true;
        }

        return false;
    }

    /// <summary>
    /// 枚举子目录时先解析链接：子目录也可能是指向远程共享的联接点，
    /// 直接探测会触发 SMB 访问并泄露凭据。
    /// </summary>
    private static List<string> EnumerateLocalDirectories(string folder)
    {
        string[] directories;
        try
        {
            directories = Directory.EnumerateDirectories(folder).Take(MaxDirectoryScan).ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"枚举目录失败（{folder}）：{ex.Message}");
            return new List<string>();
        }

        var result = new List<string>(directories.Length);
        foreach (var directory in directories)
            if (TryNormalizeLocalPath(directory, null, out var normalized))
                result.Add(normalized);

        return result;
    }

    private static bool LooksLikeGameInstance(string folder)
    {
        try
        {
            if (File.Exists(Path.Combine(folder, "appxmanifest.xml"))) return true;
            if (File.Exists(Path.Combine(folder, "MicrosoftGame.Config"))) return true;
            if (Directory.EnumerateFiles(folder, "Minecraft*.exe").Any()) return true;

            var inner = Path.Combine(folder, "inner");
            if (!TryNormalizeLocalPath(inner, null, out var innerPath) || !Directory.Exists(innerPath))
                return false;

            if (File.Exists(Path.Combine(innerPath, "appxmanifest.xml"))) return true;
            if (File.Exists(Path.Combine(innerPath, "MicrosoftGame.Config"))) return true;
            return Directory.EnumerateFiles(innerPath, "Minecraft*.exe").Any();
        }
        catch
        {
            return false;
        }
    }

    private static bool HasVersionLayout(string folder)
    {
        foreach (var rootName in new[] { "versions", "bedrock_versions" })
        {
            var versionsPath = Path.Combine(folder, rootName);
            if (!TryNormalizeLocalPath(versionsPath, null, out var versionsRoot) ||
                !Directory.Exists(versionsRoot))
                continue;

            if (HasRealVersionDirectories(versionsRoot)) return true;

            var extractedPath = Path.Combine(versionsRoot, "extracted");
            if (TryNormalizeLocalPath(extractedPath, null, out var extractedRoot) &&
                Directory.Exists(extractedRoot) && HasRealVersionDirectories(extractedRoot))
                return true;
        }

        return false;
    }

    private static bool HasRealVersionDirectories(string path)
    {
        // 只有子目录里真的存在游戏文件/清单，才算可识别的版本；
        // 否则随便一个空目录都能通过导入校验。
        return EnumerateLocalDirectories(path).Any(dir =>
        {
            var name = Path.GetFileName(dir);
            return !name.Equals("cache", StringComparison.OrdinalIgnoreCase) &&
                   !name.Equals("loaders", StringComparison.OrdinalIgnoreCase) &&
                   !name.Equals("extracted", StringComparison.OrdinalIgnoreCase) &&
                   LooksLikeGameInstance(dir);
        });
    }

    private static bool HasVersionDirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path).Any(dir =>
            {
                var name = Path.GetFileName(dir);
                return !name.Equals("cache", StringComparison.OrdinalIgnoreCase) &&
                       !name.Equals("loaders", StringComparison.OrdinalIgnoreCase);
            });
        }
        catch
        {
            return false;
        }
    }

    private static bool LocalFileExists(string path) =>
        TryNormalizeLocalPath(path, null, out var normalized) && File.Exists(normalized);

    private static bool LocalDirectoryExists(string path) =>
        TryNormalizeLocalPath(path, null, out var normalized) && Directory.Exists(normalized);

    /// <summary>
    /// 把路径规范化为真实的本地路径。解析符号链接和联接点后，
    /// 目标为远程位置（UNC 共享等）的路径会被拒绝，后续探测才不会触发 SMB 访问。
    /// </summary>
    public static bool TryNormalizeLocalPath(string? path, string? basePath, out string normalized)
    {
        normalized = string.Empty;
        var value = path?.Trim() ?? string.Empty;
        if (value.Length == 0 || value.StartsWith("\\\\") || value.StartsWith("//") ||
            value.StartsWith(@"\\?\") || value.Contains("://", StringComparison.Ordinal))
            return false;

        try
        {
            var fullPath = string.IsNullOrEmpty(basePath)
                ? Path.GetFullPath(value)
                : Path.GetFullPath(value, basePath);

            if (!TryResolveLocalTarget(fullPath, out var resolved)) return false;

            var root = Path.GetPathRoot(resolved);
            if (string.IsNullOrEmpty(root) || IsRemoteRoot(root)) return false;

            normalized = string.Equals(resolved, root, StringComparison.OrdinalIgnoreCase)
                ? resolved
                : resolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return true;
        }
        catch
        {
            normalized = string.Empty;
            return false;
        }
    }

    public static bool PathsEqual(string? left, string? right)
    {
        if (!TryNormalizeLocalPath(left, null, out var normalizedLeft) ||
            !TryNormalizeLocalPath(right, null, out var normalizedRight))
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 逐级解析符号链接与联接点，返回最终的真实本地路径。
    /// </summary>
    private static bool TryResolveLocalTarget(string fullPath, out string resolved)
    {
        resolved = fullPath;

        var root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrEmpty(root) || IsRemoteRoot(root)) return false;

        var current = root;
        var pending = new Queue<string>(SplitSegments(fullPath[root.Length..]));
        var hops = 0;

        while (pending.Count > 0)
        {
            var segment = pending.Dequeue();
            var next = Path.Combine(current, segment);

            if (!TryGetAttributes(next, out var attributes))
            {
                // 不存在的路径不可能包含链接，直接拼出剩余部分。
                current = pending.Count == 0
                    ? next
                    : Path.Combine(next, string.Join(Path.DirectorySeparatorChar, pending));
                pending.Clear();
                break;
            }

            if ((attributes & FileAttributes.ReparsePoint) == 0)
            {
                current = next;
                continue;
            }

            if (++hops > MaxReparsePointHops) return false;

            // 非链接的重解析点（例如 OneDrive 占位文件）解析不出目标，按普通路径处理。
            if (!TryResolveImmediateLink(next, out var target))
            {
                current = next;
                continue;
            }

            var targetRoot = Path.GetPathRoot(target);
            if (string.IsNullOrEmpty(targetRoot) || IsRemoteRoot(targetRoot)) return false;

            // 目标自身可能仍是链接：把它拆成路径段重新入队，继续逐级解析。
            // 只解析直接目标，因为 returnFinalTarget 会跟随整条链接链并访问远程目标。
            var remaining = pending.ToArray();
            pending.Clear();
            foreach (var item in SplitSegments(target[targetRoot.Length..])) pending.Enqueue(item);
            foreach (var item in remaining) pending.Enqueue(item);

            current = targetRoot;
        }

        resolved = current;
        return true;
    }

    private static string[] SplitSegments(string path) =>
        path.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// 解析链接的<b>直接</b>目标。注意不能使用 returnFinalTarget：
    /// 它会沿链接链继续解析，访问远程目标时实测会阻塞数十秒并触发 SMB 连接。
    /// </summary>
    private static bool TryResolveImmediateLink(string path, out string target)
    {
        target = string.Empty;

        var resolved = ResolveDirectoryLink(path) ?? ResolveFileLink(path);
        if (string.IsNullOrEmpty(resolved)) return false;

        // 目标可能带 \\?\ 前缀，去掉后才能与常规路径比较。
        if (resolved.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
        {
            target = resolved;
            return true;
        }

        if (resolved.StartsWith(@"\\?\", StringComparison.Ordinal) ||
            resolved.StartsWith(@"\??\", StringComparison.Ordinal))
            resolved = resolved[4..];

        target = resolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.Equals(resolved, Path.GetPathRoot(resolved), StringComparison.OrdinalIgnoreCase))
            target = resolved;
        return target.Length > 0;
    }

    private static string? ResolveDirectoryLink(string path)
    {
        try
        {
            return Directory.ResolveLinkTarget(path, false)?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveFileLink(string path)
    {
        try
        {
            return File.ResolveLinkTarget(path, false)?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetAttributes(string path, out FileAttributes attributes)
    {
        try
        {
            attributes = File.GetAttributes(path);
            return true;
        }
        catch
        {
            attributes = default;
            return false;
        }
    }

    private static bool IsRemoteRoot(string root) =>
        root.StartsWith("\\\\", StringComparison.Ordinal) ||
        root.StartsWith("//", StringComparison.Ordinal) ||
        root.StartsWith(@"\\?\", StringComparison.Ordinal) ||
        root.StartsWith(@"\??\", StringComparison.Ordinal) ||
        IsMappedNetworkDrive(root);

    /// <summary>
    /// 判断驱动器根目录是否为网络映射盘（例如 net use 的 Z:\）。
    /// 这类路径的根既不是 UNC 也不是设备路径，但探测时同样会向共享发送 SMB 认证。
    /// </summary>
    private static bool IsMappedNetworkDrive(string root)
    {
        if (!OperatingSystem.IsWindows()) return false;

        try
        {
            var driveRoot = Path.GetPathRoot(root);
            if (string.IsNullOrEmpty(driveRoot) || driveRoot.Length < 2 || driveRoot[1] != ':')
                return false;

            return new DriveInfo(driveRoot).DriveType == DriveType.Network;
        }
        catch
        {
            return false;
        }
    }
}
