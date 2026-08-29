using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Loaders.LeviLamina;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Loader.LeviLamina;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.Loaders.LoaderInstance;

public class LeviLamina : IModsLoader
{
    private VersionConfig _instance;
    public string LoaderName { get; } = "LeviLamina";
    public string LoaderDescription { get; } = "LeviMC 开发的基岩版模组加载器";
    public bool CanRemove { get; } = true;
    public string? IconUri { get; } = "avares://BedrockBoot/Assets/Icon/Other/LeviLauncher.png";

    public void InitLoader(VersionConfig instance)
    {
        _instance = instance;
    }

    public void PreLaunch()
    {
        var llModsFolder = Path.Combine(_instance.VersionPath!, "config", "BedrockBoot2", "levilamina", "ll.mods");
        var localModsFolder = Path.Combine(_instance.VersionPath!, "mods");

        try
        {
            if (Directory.Exists(localModsFolder)) Directory.Delete(localModsFolder, true);
            Directory.CreateSymbolicLink(localModsFolder, llModsFolder);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }

        var preLoadFile = Path.Combine(_instance.VersionPath!, "config", "BedrockBoot2", "levilamina", "preloader",
            "bin", "PreLoader.dll");
        var bedrockRuntime = Path.Combine(_instance.VersionPath!, "config", "BedrockBoot2", "levilamina",
            "bedrock_runtime_data");
        File.Copy(preLoadFile, Path.Combine(_instance.VersionPath!, "preload", "PreLoader.dll"), true);
        File.Copy(bedrockRuntime, Path.Combine(_instance.VersionPath!, "bedrock_runtime_data"), true);
    }

    public async Task<bool> ApplicableInstance()
    {
        var versionDb = await LeviLaminaVersionDb.FetchAsync();
        var allVersions = versionDb.Versions.Keys.Select(x => x.Replace(".", "").Replace("0", ""));
        var thisVersion = _instance.Info.Version.Replace(".", "").Replace("0", "");

        return allVersions.Contains(thisVersion);
    }

    public string GetInstalledVersion()
    {
        var llModsManifest = Path.Combine(_instance.VersionPath!, "config", "BedrockBoot2", "levilamina", "ll.mods",
            "LeviLamina", "manifest.json");

        var conf = new ConfigEntity<LocalManifest>(llModsManifest, false);
        return conf.Data.Version;
    }

    public bool IsInstalled()
    {
        var preLoadFile = Path.Combine(_instance.VersionPath!, "config", "BedrockBoot2", "levilamina", "preloader",
            "bin", "PreLoader.dll");
        var bedrockRuntime = Path.Combine(_instance.VersionPath!, "config", "BedrockBoot2", "levilamina",
            "bedrock_runtime_data");

        return File.Exists(preLoadFile) && File.Exists(bedrockRuntime);
    }

    public void Install()
    {
        var versionDb = LeviLaminaVersionDb.Fetch();
        var dialog = new DialogChooseLeviLaminaInstallVersionContent(versionDb.Versions[
            versionDb.Versions.Keys.First(x =>
                x.Replace(".", "").Replace("0", "") == _instance.Info.Version.Replace(".", "").Replace("0", ""))]);
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
                var toothUrl = $"https://fastly.jsdelivr.net/gh/LiteLDev/LeviLamina@v{version}/tooth.json";

                var tooth = await FetchToothJsonAsync(toothUrl);
                if (tooth != null)
                {
                    var clientDepens = tooth.Variants.First(x => x.Label == "client" && x.Platform == "win-x64");
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
                                var llModsFolder = Path.Combine(_instance.VersionPath!, "config", "BedrockBoot2",
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
                                ZipHelper.ExtractZipFile(path, Path.Combine(_instance.VersionPath!, "config",
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
                                ZipHelper.ExtractZipFile(path, Path.Combine(_instance.VersionPath!, "config",
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
                            Console.WriteLine("所有文件下载完成!");
                            DialogHost.Close();
                        }
                    );
                }
            }
        });
    }

    private async Task<ToothJson?> FetchToothJsonAsync(string url)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", $"BedrockBoot/{Global.GlobalModel.BodyVersion}");
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ToothJson>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
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
                var llModsFolder = Path.Combine(_instance.VersionPath!, "config", "BedrockBoot2", "levilamina");
                var localModsFolder = Path.Combine(_instance.VersionPath!, "mods");

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
            }
        });
    }

    public void ViewInfo()
    {
    }
}

public class LocalManifest
{
    [JsonPropertyName("version")] public string Version { get; set; }
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