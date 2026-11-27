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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.LeviLamina;
using BedrockBoot.Models.Pack.Game.Loaders.LeviLamina;
using BedrockBoot.Models.Pack.Game.Loaders.ModsManagers;
using BedrockBoot.Standard.Interface.ModLoader;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Loader;
using BedrockBoot.Views.DialogContent.Loader.LeviLamina;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.Loaders.LoaderInstance;

public class LeviLamina : IModsLoader
{
    private ConfigEntity<LeviLaminaLoaderConfig> _configEntity;
    public Action? OnUpdate { get; set; }
    public IModsManager ModsManager { get; set; }
    public string LoaderName { get; } = "LeviLamina";
    public string LoaderDescription { get; } = "LeviMC 开发的基岩版模组加载器";
    public string? LoaderWebsite { get; } = "https://levimc.org/";
    public string LoaderIntro { get; } =
        "由 LiteLDev 社区开发的基岩版原生模组加载器。它通过 PreLoader 在游戏进程启动前注入游戏，" +
        "让您可以在基岩版客户端上安装和运行 Native 模组，" +
        "BedrockBoot 会将该加载器的模组统一托管到实例的模组管理页面中。";
    public bool CanRemove { get; } = true;
    public bool IsAllowDisabling { get; } = true;
    public string? IconUri { get; } = "avares://BedrockBoot/Assets/Icon/Other/LeviLauncher.png";

    private string _configPath =>
        Path.Combine(GameInstance.VersionPath!, "config", "BedrockBoot2", "levilamina", "config.json");

    public string ModsFolder =>
        Path.Combine(GameInstance.VersionPath!, "config", "BedrockBoot2", "levilamina", "ll.mods");

    public List<AddonEvent> AddonEvents { get; set; }

    public VersionConfig GameInstance { get; set; }

    public void InitLoader(VersionConfig instance)
    {
        GameInstance = instance;
        ModsManager = new LeviLaminaModsManager() { OnRefresh = OnUpdate };
        ModsManager.Init(GameInstance);
    }

    public void PreLaunch()
    {
        var localModsFolder = Path.Combine(GameInstance.VersionPath!, "mods");

        try
        {
            if (Directory.Exists(localModsFolder)) Directory.Delete(localModsFolder, true);
            if (GetIsEnabled()) Directory.CreateSymbolicLink(localModsFolder, ModsFolder);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }

        if (!GetIsEnabled()) return;

        var preLoadFile = Path.Combine(GameInstance.VersionPath!, "config", "BedrockBoot2", "levilamina", "preloader",
            "bin", "PreLoader.dll");
        var bedrockRuntime = Path.Combine(GameInstance.VersionPath!, "config", "BedrockBoot2", "levilamina",
            "bedrock_runtime_data");
        File.Copy(preLoadFile, Path.Combine(GameInstance.VersionPath!, "preload", "PreLoader.dll"), true);
        File.Copy(bedrockRuntime, Path.Combine(GameInstance.VersionPath!, "bedrock_runtime_data"), true);
    }

    public async Task<bool> ApplicableInstance()
    {
        var versionDb = await LeviLaminaVersionDb.FetchAsync();
        var allVersions = versionDb.Versions.Keys.Select(x => x.Replace(".", "").Replace("0", ""));
        var thisVersion = GameInstance.Info.Version.Replace(".", "").Replace("0", "");

        return allVersions.Contains(thisVersion);
    }

    public string GetInstalledVersion()
    {
        var llModsManifest = Path.Combine(GameInstance.VersionPath!, "config", "BedrockBoot2", "levilamina", "ll.mods",
            "LeviLamina", "manifest.json");

        var conf = new ConfigEntity<LocalManifest>(llModsManifest, false);
        return conf.Data.Version;
    }

    public bool IsInstalled() => IsInstallModLoader(GameInstance.VersionPath!);

    public void Install()
    {
        var versionDb = LeviLaminaVersionDb.Fetch();
        var dialog = new DialogChooseLeviLaminaInstallVersionContent(versionDb.Versions[
            versionDb.Versions.Keys.First(x =>
                x.Replace(".", "").Replace("0", "") == GameInstance.Info.Version.Replace(".", "").Replace("0", ""))]);
        DialogHost.Show(new()
        {
            Title = "选择安装的版本",
            Content = dialog,
            CloseButtonText = "安装",
            PrimaryButtonText = "取消",
            CloseAction = async () =>
            {
                var version = dialog.ChooseVersion;
                Console.WriteLine($@"将要安装的 LeviLamina 版本：{version}");
                var releaseUrl =
                    $"https://github.com/LiteLDev/LeviLamina/releases/download/v{version}/levilamina-v{version}-client-release-windows-x64.zip";

                // 拉取清单期间显示加载框，避免长时间无响应
                var loadingText = new TextBlock
                {
                    FontSize = 16,
                    Text = $"正在获取 LeviLamina v{version} 清单…"
                };
                var loadingRing = new OnePointUI.Avalonia.Styling.Controls.OnePointControls.ProgressRing
                {
                    Width = 36,
                    Height = 36,
                    Background = Avalonia.Media.Brushes.Transparent
                };
                var loadingPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Children = { loadingRing, loadingText }
                };
                loadingText.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                loadingText.Margin = new Avalonia.Thickness(12, 0, 0, 0);

                Dispatcher.UIThread.Invoke(() => DialogHost.Show(new DialogInfo
                {
                    Title = "正在安装 LeviLamina",
                    Content = loadingPanel
                }));

                // 清单走启动器的 GitHub 加速源下载（与主程序下载一致），不使用 jsdelivr 直链；
                // lip.toml（新格式）与 tooth.json（旧格式）并行竞速，先成功者胜出
                var tooth = await FetchToothJsonAsync(version);

                Dispatcher.UIThread.Invoke(DialogHost.Close);

                if (tooth != null)
                {
                    // 新 lip.toml：变体共用空 label，靠 side = "client" 区分；旧 tooth.json：label = "client"
                    var clientDepens = tooth.Variants.First(x =>
                        (x.Side == "client" || x.Label == "client") && x.Platform == "win-x64");
                    var preLoad = clientDepens.Dependencies["github.com/LiteLDev/PreLoader"];
                    var bedrockRuntime = clientDepens.Dependencies["github.com/LiteLDev/bedrock-runtime-data"];

                    var preLoadUrl =
                        $"https://github.com/LiteLDev/PreLoader/releases/download/v{preLoad}/preloader-v{preLoad}-windows-x64.zip";
                    var bedrockRuntimeUrl =
                        $"https://github.com/LiteLDev/bedrock-runtime-data/releases/download/v{bedrockRuntime}/bedrock-runtime-data-v{bedrockRuntime}-windows-x64.zip";

                    Console.WriteLine(releaseUrl);
                    Console.WriteLine(preLoadUrl);
                    Console.WriteLine(bedrockRuntimeUrl);

                    var downloadDialog = new DialogDownloadManagerContent();

                    var filesToDownload = new List<DownloadFileTask>
                    {
                        new()
                        {
                            Name = "LeviLamina",
                            Url = releaseUrl,
                            SavePath = Path.Combine(PathsList.TempPath,
                                $"levilamina-v{version}-client-release-windows-x64.zip"),
                            OnComplete = (path) =>
                            {
                                var llModsFolder = Path.Combine(GameInstance.VersionPath!, "config", "BedrockBoot2",
                                    "levilamina", "ll.mods");
                                ZipHelper.ExtractZipFile(path, llModsFolder, true);
                            }
                        },
                        new()
                        {
                            Name = "PreLoader",
                            Url = preLoadUrl,
                            SavePath = Path.Combine(PathsList.TempPath, $"preloader-v{preLoad}-windows-x64.zip"),
                            OnComplete = (path) =>
                            {
                                ZipHelper.ExtractZipFile(path, Path.Combine(GameInstance.VersionPath!, "config",
                                    "BedrockBoot2",
                                    "levilamina",
                                    "preloader"), true);
                            }
                        },
                        new()
                        {
                            Name = "BedrockRuntime",
                            Url = bedrockRuntimeUrl,
                            SavePath = Path.Combine(PathsList.TempPath,
                                $"bedrock-runtime-data-v{bedrockRuntime}-windows-x64.zip"),
                            OnComplete = (path) =>
                            {
                                ZipHelper.ExtractZipFile(path, Path.Combine(GameInstance.VersionPath!, "config",
                                    "BedrockBoot2",
                                    "levilamina"), true);
                            }
                        }
                    };

                    DialogHost.Show(new()
                    {
                        Title = "下载 LeviLamina",
                        Content = downloadDialog
                    });

                    await downloadDialog.StartDownloadAsync(
                        filesToDownload,
                        async (url, savePath, progress) =>
                        {
                            var downloader = new GithubFilesDownloader();
                            return await downloader.DownloadAsync(url, savePath, progress);
                        },
                        (downloadedFiles) =>
                        {
                            Console.WriteLine(@"所有文件下载完成!");
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                OnUpdate?.Invoke();
                                DialogHost.Close();
                            });
                        }
                    );
                    OnUpdate?.Invoke();
                }
                else
                {
                    // lip.toml 与 tooth.json 均获取失败
                    DialogHost.Show(new()
                    {
                        Title = "安装失败",
                        Content = $"无法获取 LeviLamina v{version} 的清单文件（lip.toml / tooth.json 均不可达），请检查网络后重试。",
                        CloseButtonText = "确定",
                        AccountButton = DialogButtons.CloseButton
                    });
                }
            }
        });
    }

    // 拉取 LeviLamina 清单并解析：lip.toml（新格式）与 tooth.json（旧格式）并行竞速，先成功者胜出。
    // 下载统一走 GithubFilesDownloader（SourceList 加速源竞速），不使用 jsdelivr 直链
    private static async Task<ToothJson> FetchToothJsonAsync(string version)
    {
        try
        {
            var downloader = new GithubFilesDownloader();

            var fetchTasks = new[] { "lip.toml", "tooth.json" }
                .Select(fileName => FetchManifestTextAsync(downloader, version, fileName))
                .ToList();

            // 竞速：任一候选成功即用；全失败则返回 null
            while (fetchTasks.Count > 0)
            {
                var completed = await Task.WhenAny(fetchTasks);

                try
                {
                    var text = await completed;
                    if (string.IsNullOrWhiteSpace(text)) throw new Exception("清单内容为空");

                    // lip.toml（新）解析失败（含 JSON 文本）就地回落 tooth.json（旧）反序列化
                    var tooth = ToToothJson(LipToml.Parse(text))
                                ?? JsonSerializer.Deserialize<ToothJson>(text, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                    if (tooth?.Variants is { Length: > 0 }) return tooth;

                    throw new Exception("清单解析失败或无可用变体");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"清单候选失败，等待其余候选: {ex.Message}");
                    fetchTasks.Remove(completed);
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // 下载单个清单文件内容（临时文件即取即删）
    private static async Task<string> FetchManifestTextAsync(
        GithubFilesDownloader downloader, string version, string fileName)
    {
        var tempFile = Path.Combine(PathsList.TempPath, $"levilamina-manifest-{Guid.NewGuid():N}-{fileName}");
        try
        {
            var url = $"https://github.com/LiteLDev/LeviLamina/raw/v{version}/{fileName}";
            await downloader.DownloadAsync(url, tempFile, null, CancellationToken.None);
            return await File.ReadAllTextAsync(tempFile);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            catch
            {
            }
        }
    }

    // LipToml.Parse 产出 LeviConfig，这里转成本文件使用的 ToothJson 模型
    private static ToothJson ToToothJson(LeviConfig config)
    {
        if (config == null) return null;

        return new ToothJson
        {
            FormatVersion = config.FormatVersion,
            Tooth = config.Tooth,
            Version = config.Version,
            Info = config.Info == null
                ? null
                : new ToothInfo
                {
                    Name = config.Info.Name,
                    Description = config.Info.Description,
                    Tags = config.Info.Tags?.ToArray(),
                    AvatarUrl = config.Info.AvatarUrl
                },
            Variants = config.Variants?.Select(v => new ToothVariant
            {
                Label = v.Label,
                Side = v.Side,
                Platform = v.Platform,
                Dependencies = v.Dependencies,
                Assets = v.Assets?.Select(a => new ToothAsset
                {
                    Type = a.Type,
                    Urls = a.Urls?.ToArray(),
                    Placements = a.Placements?.Select(p => new ToothPlacement
                    {
                        Type = p.Type,
                        Src = p.Src,
                        Dest = p.Dest
                    }).ToArray()
                }).ToArray(),
                RemoveFiles = v.RemoveFiles?.ToArray(),
                Scripts = v.Scripts == null
                    ? null
                    : new ToothScripts
                    {
                        PostInstall = v.Scripts.PostInstall?.ToArray(),
                        PostUninstall = v.Scripts.PostUninstall?.ToArray()
                    }
            }).ToArray()
        };
    }

    public void Remove()
    {
        DialogHost.Show(new()
        {
            Title = "删除 LeviLamina",
            Content = "您确认要删除 LeviLamina 加载器吗？\n" +
                      "删除后将会连同其 Mods 一同删除。",
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var llModsFolder = Path.Combine(GameInstance.VersionPath!, "config", "BedrockBoot2", "levilamina");
                var localModsFolder = Path.Combine(GameInstance.VersionPath!, "mods");

                try
                {
                    if (Directory.Exists(localModsFolder)) Directory.Delete(localModsFolder, true);
                }
                catch
                {
                }

                try
                {
                    if (Directory.Exists(llModsFolder)) Directory.Delete(llModsFolder, true);
                }
                catch
                {
                }

                OnUpdate?.Invoke();
            }
        });
    }

    public void ViewInfo()
    {
        DialogLoaderViewContent.Show(this);
    }

    public bool GetIsEnabled()
    {
        if (_configEntity == null)
            _configEntity = new(_configPath);

        if (!IsInstalled())
            return false;

        return _configEntity.Data.IsEnable;
    }

    public void SetIsEnabled(bool isEnabled)
    {
        if (_configEntity == null)
            _configEntity = new(_configPath);

        _configEntity.Data.IsEnable = isEnabled;
        _configEntity.Save();
    }

    public static bool IsInstallModLoader(string path)
    {
        var preLoadFile = Path.Combine(path, "config", "BedrockBoot2", "levilamina", "preloader",
            "bin", "PreLoader.dll");
        var bedrockRuntime = Path.Combine(path, "config", "BedrockBoot2", "levilamina",
            "bedrock_runtime_data");
        var levilaminaMainFile = Path.Combine(path, "config", "BedrockBoot2", "levilamina",
            "ll.mods", "LeviLamina", "LeviLamina.dll");

        return File.Exists(preLoadFile) && File.Exists(bedrockRuntime) && File.Exists(levilaminaMainFile);
    }
}

public class LeviLaminaLoaderConfig
{
    [JsonPropertyName("isEnable")] public bool IsEnable { get; set; } = true;
}

public class ToothJson
{
    [JsonPropertyName("format_version")] public int FormatVersion { get; set; }
    [JsonPropertyName("format_uuid")] public string FormatUuid { get; set; }
    [JsonPropertyName("tooth")] public string Tooth { get; set; }
    [JsonPropertyName("version")] public string Version { get; set; }
    [JsonPropertyName("info")] public ToothInfo Info { get; set; }
    [JsonPropertyName("variants")] public ToothVariant[] Variants { get; set; }
}

public class ToothInfo
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; }
    [JsonPropertyName("tags")] public string[] Tags { get; set; }
    [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }
}

public class ToothVariant
{
    [JsonPropertyName("label")] public string? Label { get; set; }

    // lip.toml（format = 4）新增：区分 client / server，替代旧的 label = "client" 约定
    [JsonPropertyName("side")] public string? Side { get; set; }

    [JsonPropertyName("platform")] public string Platform { get; set; }
    [JsonPropertyName("dependencies")] public Dictionary<string, string> Dependencies { get; set; }
    [JsonPropertyName("assets")] public ToothAsset[] Assets { get; set; }
    [JsonPropertyName("remove_files")] public string[] RemoveFiles { get; set; }
    [JsonPropertyName("scripts")] public ToothScripts Scripts { get; set; }
}

public class ToothAsset
{
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("urls")] public string[] Urls { get; set; }
    [JsonPropertyName("placements")] public ToothPlacement[] Placements { get; set; }
}

public class ToothPlacement
{
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("src")] public string Src { get; set; }
    [JsonPropertyName("dest")] public string Dest { get; set; }
}

public class ToothScripts
{
    [JsonPropertyName("post_install")] public string[] PostInstall { get; set; }
    [JsonPropertyName("post_uninstall")] public string[] PostUninstall { get; set; }
}