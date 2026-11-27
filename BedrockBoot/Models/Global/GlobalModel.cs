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
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BedrockBoot.Standard.Entity;
using BedrockBoot.Standard.Entity.Config;
using BedrockBoot.Standard.Entity.Info.Xbox;
using BedrockBoot.Standard.Entity.Manifest;
using BedrockBoot.Standard.Enum.Type;
using BedrockBoot.GravityCone;
using BedrockBoot.GravityCone.Entry;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.Windows;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Global;

public class GlobalModel
{
    public static GravityConeClient? GravityConeClient { get; set; } = null;
    public static RoomState? CurrentRoomState { get; set; } = null;
    public static MainWindow MainWindow;
    public static TaskManager TaskManager { get; set; } = new();
    public static bool IsAbleToLaunchGame { get; set; } = false;
    public static bool IsProgressRunning { get; set; } = false;
    public static FunctionOptionEntry FunctionOption { get; set; }
    public static CustomManifest CustomManifest { get; set; } = new();

    public static string BodyVersion =>
        $"{Assembly.GetExecutingAssembly().GetName().Version!.ToString()}-{CheckUpdate.GetBodyUpdateType()}";

    public static Action? MainPageUpdateInstance { get; set; }

    public static PaperConnectCore PaperConnectCore { get; set; }
    public static List<string> ETPublicServer { get; set; }
    public static XboxUserInfo XboxUserInfo { get; set; }
    public static AppRunType AppRunType { get; set; } = AppRunType.Default;
    public static bool IsNetworkAvailable { get; set; }
    public static ArchiveBackup ArchiveBackup { get; } = new();

    public static List<OtherLauncherInfo> OtherLauncher = new()
    {
        new OtherLauncherInfo() // BMCBL
        {
            Name = "BMCBL",
            IconUrl = "avares://BedrockBoot/Assets/Icon/Other/BMCBL.png",
            IsExists = false,
            OnImport = async _ =>
            {
                var storageProvider = TopLevel.GetTopLevel(GlobalModel.MainWindow);
                if (storageProvider == null)
                {
                    Console.WriteLine(@"无法获取主窗口的顶层存储提供程序");
                    return;
                }

                var options = new FilePickerOpenOptions
                {
                    Title = "选择 BMCBL 本体",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("EXE 可执行文件")
                        {
                            Patterns = new[] { "*.exe" }
                        }
                    }
                };

                var files = await storageProvider.StorageProvider.OpenFilePickerAsync(options);
                if (files != null && files.Count > 0)
                {
                    var selectedFile = files[0];

                    var filePath = selectedFile.Path.LocalPath;
                    var folder = Path.Combine(Path.GetDirectoryName(filePath), "BMCBL");
                    var realPath = Path.Combine(folder, "versions");
                    if (Directory.Exists(folder))
                    {
                        /*var inPath = Path.Combine(folder, GameInfoHelper.GetGameFolderRootName(currentFolder.GameFolderPath));
                        if (Directory.Exists(realPath) &&
                            !Directory.Exists(inPath))
                        {
                            Directory.CreateSymbolicLink(inPath, realPath);
                        }*/

                        BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                        {
                            GameFolderName = "BMCBL",
                            GameFolderPath = folder,
                            GameFolderType = GameFolderType.BMCBL
                        });
                        BedrockBoot.Core.Global.GlobalModel.Config.Save();

                        MainWindow?.CloseDraw();
                        DialogHost.Show(new DialogInfo
                        {
                            Title = "导入成功",
                            Content = "导入 BMCBL 启动器的配置成功",
                            CloseButtonText = "确定"
                        });
                    }
                }
            }
        },
        new OtherLauncherInfo() // LeviLauncher
        {
            Name = "LeviLauncher",
            IconUrl = "avares://BedrockBoot/Assets/Icon/Other/LeviLauncher.png",
            ConfigFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "levilauncher.exe",
                "config.json"
            ),
            OnImport = s =>
            {
                var conf = new ConfigEntity<ConfigLeviLauncher>(s, false);
                /*var realPath = Path.Combine(conf.Data.BaseRoot, "versions");
                var inPath = Path.Combine(conf.Data.BaseRoot, GameInfoHelper.GetGameFolderRootName(currentFolder.GameFolderPath));
                if (!Directory.Exists(realPath)) Directory.CreateDirectory(realPath);

                if (!Directory.Exists(inPath)) Directory.CreateSymbolicLink(inPath, realPath);*/
                BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                {
                    GameFolderName = "LeviLauncher",
                    GameFolderPath = conf.Data.BaseRoot,
                    GameFolderType = GameFolderType.LeviLauncher
                });
                BedrockBoot.Core.Global.GlobalModel.Config.Save();

                GlobalModel.MainWindow.CloseDraw();
                DialogHost.Show(new DialogInfo
                {
                    Title = "导入成功",
                    Content = "导入 LeviLauncher 启动器的配置成功",
                    CloseButtonText = "确定"
                });
            }
        },
        new OtherLauncherInfo() // PCLSE
        {
            Name = "PCLSE",
            IconUrl = "avares://BedrockBoot/Assets/Icon/Other/PCLSE.png",
            ConfigFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PCLSE",
                "config.v1.json"),
            IsExists = false,
            OnImport = async _ =>
            {
                try
                {
                    await ImportPclSeAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"导入 PCLSE 配置失败：{ex}");
                    DialogHost.Show(new DialogInfo
                    {
                        Title = "导入失败",
                        Content = $"读取 PCLSE 配置时发生错误：{ex.Message}",
                        CloseButtonText = "确定"
                    });
                }
            }
        },
        new OtherLauncherInfo() // Portal
        {
        }
    };

    private static async System.Threading.Tasks.Task ImportPclSeAsync()
    {
        var storageProvider = TopLevel.GetTopLevel(MainWindow);
        if (storageProvider == null)
        {
            Console.WriteLine(@"无法获取主窗口的顶层存储提供程序");
            return;
        }

        var files = await storageProvider.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择 PCLSE 本体",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PCLSE 可执行文件")
                {
                    Patterns = new[] { "*.exe" }
                }
            }
        });
        if (files.Count == 0) return;

        var launcherPath = files[0].Path.LocalPath;
        var launcherRoot = Path.GetDirectoryName(launcherPath);
        if (string.IsNullOrWhiteSpace(launcherRoot)) return;

        // PCLSE 的 exe 可能位于 launcher、PCL 或发布包子目录中；逐级尝试。
        var sources = GetPclSeRoots(launcherRoot)
            .SelectMany(ReadPclSeDirectories)
            .ToList();
        var candidates = new List<string>();
        foreach (var source in sources)
        {
            var folder = PreparePclSeFolder(source);
            if (!string.IsNullOrEmpty(folder) &&
                !candidates.Contains(folder, StringComparer.OrdinalIgnoreCase))
                candidates.Add(folder);
        }

        // 已经导入过的目录也加入候选，重复点击时不再误报失败。
        var registeredFolders = BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders;
        foreach (var folder in registeredFolders.Where(folder =>
                     folder.GameFolderType == GameFolderType.PCLSE ||
                     string.Equals(folder.GameFolderName, "PCLSE", StringComparison.OrdinalIgnoreCase)))
        {
            if (TryNormalizeLocalPath(folder.GameFolderPath, null, out var normalized) &&
                !candidates.Any(candidate => PathsEqual(candidate, normalized)))
                candidates.Add(normalized);
        }

        var additions = new List<GameFolderInfo>();
        foreach (var candidate in candidates)
        {
            if (!IsLocalPath(candidate) || !Directory.Exists(candidate) ||
                registeredFolders.Any(folder => PathsEqual(folder.GameFolderPath, candidate))) continue;

            additions.Add(new GameFolderInfo
            {
                GameFolderName = "PCLSE",
                GameFolderPath = candidate,
                GameFolderType = GameFolderType.PCLSE
            });
        }

        var validExisting = candidates.Any(candidate =>
            IsLocalPath(candidate) &&
            registeredFolders.Any(folder => PathsEqual(folder.GameFolderPath, candidate)) &&
            Directory.Exists(candidate));
        if (additions.Count == 0 && !validExisting)
        {
            DialogHost.Show(new DialogInfo
            {
                Title = "导入失败",
                Content = "没有找到 PCLSE 的基岩版版本目录",
                CloseButtonText = "确定"
            });
            return;
        }

        try
        {
            registeredFolders.AddRange(additions);
            BedrockBoot.Core.Global.GlobalModel.Config.Save();
        }
        catch
        {
            foreach (var folder in additions) registeredFolders.Remove(folder);
            throw;
        }

        MainWindow?.CloseDraw();
        DialogHost.Show(new DialogInfo
        {
            Title = additions.Count > 0 ? "导入成功" : "目录已存在",
            Content = additions.Count > 0
                ? $"已导入 PCLSE 的 {additions.Count} 个基岩版目录"
                : "PCLSE 的基岩版目录已经导入，无需重复添加",
            CloseButtonText = "确定"
        });
    }

    private static IEnumerable<string> GetPclSeRoots(string launcherRoot)
    {
        if (!TryNormalizeLocalPath(launcherRoot, null, out var current)) yield break;

        for (var i = 0; i < 5 && !string.IsNullOrWhiteSpace(current); i++)
        {
            var configPath = Path.Combine(current, "PCL", "config.v1.yml");
            if (File.Exists(configPath)) yield return current;

            var parent = Directory.GetParent(current)?.FullName;
            if (string.Equals(parent, current, StringComparison.OrdinalIgnoreCase)) break;
            current = parent ?? string.Empty;
        }
    }

    private static string? PreparePclSeFolder(string source)
    {
        try
        {
            if (!TryNormalizeLocalPath(source, null, out source) || !Directory.Exists(source)) return null;

            var leafName = Path.GetFileName(source);
            if (leafName.Equals("versions", StringComparison.OrdinalIgnoreCase) ||
                leafName.Equals("bedrock_versions", StringComparison.OrdinalIgnoreCase))
                return Directory.GetParent(source)?.FullName;

            if (Directory.Exists(Path.Combine(source, "versions")) ||
                Directory.Exists(Path.Combine(source, "bedrock_versions")))
                return source;

            if (leafName.Equals("extracted", StringComparison.OrdinalIgnoreCase) &&
                Directory.GetParent(source) is { } versionsDirectory &&
                (Path.GetFileName(versionsDirectory.FullName).Equals("versions", StringComparison.OrdinalIgnoreCase) ||
                 Path.GetFileName(versionsDirectory.FullName).Equals("bedrock_versions", StringComparison.OrdinalIgnoreCase)))
            {
                return Directory.GetParent(versionsDirectory.FullName)?.FullName;
            }

            return HasVersionDirectories(source) ? source : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"适配 PCLSE 目录失败（{source}）：{ex.Message}");
        }

        return null;
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

    private static List<string> ReadPclSeDirectories(string launcherRoot)
    {
        var configPath = Path.Combine(launcherRoot, "PCL", "config.v1.yml");
        var result = new List<string>();
        if (!File.Exists(configPath)) return result;

        var values = ReadPclSeYaml(configPath);

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

        if (values.TryGetValue("BedrockFolder", out var folders) && folders.Count > 0 &&
            !string.IsNullOrWhiteSpace(folders[0]))
            Add(folders[0]);
        else
        {
            // PCLSE 现在默认使用 .minecraft\\bedrock，旧版本则使用
            // .minecraft\\bedrock_versions 或启动器根目录下的 bedrock_versions。
            Add(Path.Combine(launcherRoot, ".minecraft", "bedrock"));
            Add(Path.Combine(launcherRoot, ".minecraft", "bedrock_versions"));
            Add(Path.Combine(launcherRoot, "bedrock_versions"));
        }

        if (values.TryGetValue("BedrockExternalFolders", out var externalFolders))
            foreach (var externalValue in externalFolders)
                foreach (var external in externalValue.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    Add(external.Trim());

        return result;
    }

    private static Dictionary<string, List<string>> ReadPclSeYaml(string path)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string? sequenceKey = null;
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

            if (trimmed.StartsWith("- ") && sequenceKey != null)
            {
                result[sequenceKey].Add(ParseYamlValue(trimmed[2..].Trim()));
                continue;
            }

            var separator = trimmed.IndexOf(':');
            if (separator <= 0) continue;

            var key = trimmed[..separator].Trim();
            var value = trimmed[(separator + 1)..].Trim();
            var values = result[key] = new List<string>();
            sequenceKey = string.IsNullOrEmpty(value) ? key : null;
            if (value.StartsWith("[") && value.EndsWith("]"))
                foreach (var item in value[1..^1].Split(',', StringSplitOptions.RemoveEmptyEntries))
                    values.Add(ParseYamlValue(item.Trim()));
            else if (!string.IsNullOrEmpty(value))
                values.Add(ParseYamlValue(value));
        }

        return result;
    }

    private static string ParseYamlValue(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value[1..^1].Replace("\\\\", "\\").Replace("\\\"", "\"");
        if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
            return value[1..^1].Replace("''", "'");
        return value;
    }

    private static bool IsLocalPath(string path) => TryNormalizeLocalPath(path, null, out _);

    private static bool TryNormalizeLocalPath(string path, string? basePath, out string normalized)
    {
        normalized = string.Empty;
        var value = path.Trim();
        if (value.Length == 0 || value.StartsWith("\\\\") || value.StartsWith("//") ||
            value.StartsWith(@"\\?\") || value.Contains("://", StringComparison.Ordinal))
            return false;

        try
        {
            normalized = string.IsNullOrEmpty(basePath)
                ? Path.GetFullPath(value)
                : Path.GetFullPath(value, basePath);
            var root = Path.GetPathRoot(normalized);
            if (string.IsNullOrEmpty(root) || root.StartsWith("\\\\") || root.StartsWith("//") ||
                root.StartsWith(@"\\?\"))
            {
                normalized = string.Empty;
                return false;
            }

            if (!string.Equals(normalized, root, StringComparison.OrdinalIgnoreCase))
                normalized = normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return true;
        }
        catch
        {
            normalized = string.Empty;
            return false;
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        if (!TryNormalizeLocalPath(left, null, out var normalizedLeft) ||
            !TryNormalizeLocalPath(right, null, out var normalizedRight))
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }
}
