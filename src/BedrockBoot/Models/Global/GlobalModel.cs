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
            OnImport = async _ => await ImportPclSeAsync()
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

        // PCLSE 的 exe 可能位于 launcher、PCL 或发布包子目录中；逐级尝试，
        // 同时把已经登记过的 PCLSE 目录加入候选，避免重复导入时误报失败。
        var candidates = GetPclSeRoots(launcherRoot)
            .SelectMany(ReadPclSeDirectories)
            .ToList();
        var existingPclSeFolders = BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders
            .Where(folder => string.Equals(folder.GameFolderName, "PCLSE", StringComparison.OrdinalIgnoreCase) ||
                             folder.GameFolderType == GameFolderType.PCLSE)
            .ToList();
        foreach (var folder in existingPclSeFolders)
        {
            folder.GameFolderType = GameFolderType.PCLSE;
            if (!candidates.Contains(folder.GameFolderPath, StringComparer.OrdinalIgnoreCase))
                candidates.Add(folder.GameFolderPath);
        }
        var imported = 0;
        foreach (var candidate in candidates)
        {
            if (!Directory.Exists(candidate) ||
                BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders.Any(folder =>
                    string.Equals(folder.GameFolderPath, candidate, StringComparison.OrdinalIgnoreCase))) continue;

            BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
            {
                GameFolderName = "PCLSE",
                GameFolderPath = candidate,
                GameFolderType = GameFolderType.PCLSE
            });
            imported++;
        }

        var validExisting = existingPclSeFolders.Count(folder => Directory.Exists(folder.GameFolderPath));
        if (imported == 0 && validExisting == 0)
        {
            DialogHost.Show(new DialogInfo
            {
                Title = "导入失败",
                Content = "没有找到 PCLSE 的基岩版版本目录",
                CloseButtonText = "确定"
            });
            return;
        }

        BedrockBoot.Core.Global.GlobalModel.Config.Save();
        MainWindow?.CloseDraw();
        DialogHost.Show(new DialogInfo
        {
            Title = imported > 0 ? "导入成功" : "目录已存在",
            Content = imported > 0
                ? $"已导入 PCLSE 的 {imported} 个基岩版目录"
                : "PCLSE 的基岩版目录已经导入，无需重复添加",
            CloseButtonText = "确定"
        });
    }

    private static IEnumerable<string> GetPclSeRoots(string launcherRoot)
    {
        var current = Path.GetFullPath(launcherRoot);
        for (var i = 0; i < 5 && !string.IsNullOrWhiteSpace(current); i++)
        {
            yield return current;
            var parent = Directory.GetParent(current)?.FullName;
            if (string.Equals(parent, current, StringComparison.OrdinalIgnoreCase)) break;
            current = parent ?? string.Empty;
        }
    }

    private static List<string> ReadPclSeDirectories(string launcherRoot)
    {
        var configPath = Path.Combine(launcherRoot, "PCL", "config.v1.yml");
        var values = File.Exists(configPath)
            ? ReadPclSeYaml(configPath)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var fullPath = Path.GetFullPath(path, launcherRoot)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!result.Contains(fullPath, StringComparer.OrdinalIgnoreCase)) result.Add(fullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取 PCLSE 基岩目录失败（{path}）：{ex.Message}");
            }
        }

        if (values.TryGetValue("BedrockFolder", out var folder) && !string.IsNullOrWhiteSpace(folder))
            Add(folder);
        else
        {
            // PCLSE 现在默认使用 .minecraft\\bedrock，旧版本则使用
            // .minecraft\\bedrock_versions 或启动器根目录下的 bedrock_versions。
            Add(Path.Combine(launcherRoot, ".minecraft", "bedrock"));
            Add(Path.Combine(launcherRoot, ".minecraft", "bedrock_versions"));
            Add(Path.Combine(launcherRoot, "bedrock_versions"));
        }

        if (values.TryGetValue("BedrockExternalFolders", out var externalFolders))
            foreach (var external in externalFolders.Split(';', StringSplitOptions.RemoveEmptyEntries)) Add(external);

        return result;
    }

    private static Dictionary<string, string> ReadPclSeYaml(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            var separator = trimmed.IndexOf(':');
            if (separator <= 0) continue;

            var key = trimmed[..separator].Trim();
            var value = trimmed[(separator + 1)..].Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                var isDoubleQuoted = value[0] == '"';
                value = value[1..^1];
                if (isDoubleQuoted)
                    value = value.Replace("\\\\", "\\").Replace("\\\"", "\"");
            }
            result[key] = value;
        }

        return result;
    }
}
