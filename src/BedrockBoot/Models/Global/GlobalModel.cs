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
using BedrockBoot.Core.Models.Helper;
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
                    Title = I18nManager.Instance["Global.ChooseBmcbl"],
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType(I18nManager.Instance["FileType.Exe"])
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
                            Title = I18nManager.Instance["Global.Import.Success.Title"],
                            Content = I18nManager.Instance["Global.Import.Bmcbl.Success"],
                            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
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
                    Title = I18nManager.Instance["Global.Import.Success.Title"],
                    Content = I18nManager.Instance["Global.Import.Levi.Success"],
                    CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"]
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

    /// <summary>
    /// 导入 PCLSE 的基岩版目录。异常由 OnImport 捕获并提示，不能从 async void 中逸出。
    /// </summary>
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
        var sources = PclSeHelper.GetRoots(launcherRoot)
            .SelectMany(PclSeHelper.ReadDirectories)
            .ToList();
        var candidates = new List<string>();
        foreach (var source in sources)
        {
            var folder = PclSeHelper.PrepareFolder(source);
            if (!string.IsNullOrEmpty(folder) &&
                !candidates.Contains(folder, StringComparer.OrdinalIgnoreCase))
                candidates.Add(folder);
        }

        // 已经导入过的目录也加入候选，重复点击时不再误报失败。
        var registeredFolders = BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders;
        foreach (var folder in registeredFolders.Where(PclSeHelper.IsPclSeRegistration))
        {
            if (PclSeHelper.TryNormalizeLocalPath(folder.GameFolderPath, null, out var normalized) &&
                !candidates.Any(candidate => PclSeHelper.PathsEqual(candidate, normalized)))
                candidates.Add(normalized);
        }

        var additions = new List<GameFolderInfo>();
        var updates = new List<(GameFolderInfo Folder, string Name, GameFolderType Type)>();
        var validExisting = false;

        foreach (var candidate in candidates)
        {
            // 解析链接后的真实路径；指向 UNC 共享等远程位置的候选会被拒绝，避免触发 SMB 访问。
            if (!PclSeHelper.TryNormalizeLocalPath(candidate, null, out var normalized) ||
                !Directory.Exists(normalized))
                continue;

            // 空目录或不包含可识别版本布局的目录不能算作导入成功。
            if (!PclSeHelper.IsUsableFolder(normalized)) continue;

            var existing = registeredFolders.FirstOrDefault(folder =>
                PclSeHelper.PathsEqual(folder.GameFolderPath, normalized));
            if (existing == null)
            {
                // 同一个目录可能通过不同写法多次进入候选，避免重复注册。
                if (additions.Any(folder => PclSeHelper.PathsEqual(folder.GameFolderPath, normalized)))
                    continue;

                additions.Add(new GameFolderInfo
                {
                    GameFolderName = "PCLSE",
                    GameFolderPath = normalized,
                    GameFolderType = GameFolderType.PCLSE
                });
                continue;
            }

            if (existing.GameFolderType == GameFolderType.PCLSE &&
                string.Equals(existing.GameFolderName, "PCLSE", StringComparison.OrdinalIgnoreCase))
            {
                validExisting = true;
                continue;
            }

            // 手动添加的目录保留默认的 BedrockBoot 类型，扫描器会去找 bedrock_versions；
            // 命中同一路径时把名称和类型修正为 PCLSE，才能真正扫描到 versions 下的实例。
            if (updates.Any(change => ReferenceEquals(change.Folder, existing))) continue;

            updates.Add((existing, existing.GameFolderName, existing.GameFolderType));
            existing.GameFolderName = "PCLSE";
            existing.GameFolderType = GameFolderType.PCLSE;
        }

        if (additions.Count == 0 && updates.Count == 0 && !validExisting)
        {
            DialogHost.Show(new DialogInfo
            {
                Title = "导入失败",
                Content = "没有找到 PCLSE 的基岩版版本目录，目录为空或其中没有可识别的游戏版本",
                CloseButtonText = "确定"
            });
            return;
        }

        try
        {
            // 先写盘再提示：保存失败时回滚内存中的修改，避免出现「本次可见、重启消失」的导入项。
            registeredFolders.AddRange(additions);
            BedrockBoot.Core.Global.GlobalModel.Config.Save();
        }
        catch
        {
            foreach (var folder in additions) registeredFolders.Remove(folder);
            foreach (var (folder, name, type) in updates)
            {
                folder.GameFolderName = name;
                folder.GameFolderType = type;
            }

            throw;
        }

        var messages = new List<string>();
        if (additions.Count > 0) messages.Add($"已导入 PCLSE 的 {additions.Count} 个基岩版目录");
        if (updates.Count > 0) messages.Add($"已将 {updates.Count} 个已有目录修正为 PCLSE 类型");

        MainWindow?.CloseDraw();
        DialogHost.Show(new DialogInfo
        {
            Title = messages.Count > 0 ? "导入成功" : "目录已存在",
            Content = messages.Count > 0
                ? string.Join("；", messages)
                : "PCLSE 的基岩版目录已经导入，无需重复添加",
            CloseButtonText = "确定"
        });
    }

    /// <summary>
    /// PCLSE 目录导入的辅助逻辑：受限的 YAML 读取、路径校验与版本布局探测。
    /// 与界面无关，便于独立测试。
    /// </summary>
}
