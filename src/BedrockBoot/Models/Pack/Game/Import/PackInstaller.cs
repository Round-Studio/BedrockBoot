using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Windows.Management.Deployment;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Import;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.Utils;

namespace BedrockBoot.Models.Pack.Game.Import;

public class PackInstaller
{
    public string PackFile { get; set; }
    public MinecraftBuildTypeVersion GameBuildType { get; set; }
    public IProgress<PackImportProgress> ImportProgress { get; set; } = new Progress<PackImportProgress>();
    public PackInstaller(string filePath)
    {
        PackFile = filePath;
    }

    public async System.Threading.Tasks.Task Install(string dir, string gameName)
    {
        ImportProgress.Report(new PackImportProgress() { Progress = 10, StatusMessage = "判断文件类型..." });
        GameBuildType = PackAnalysis.GetPackBuildTypeWithFileHeader(PackFile);

        ImportProgress.Report(new PackImportProgress() { Progress = 100, StatusMessage = "判断文件类型完毕" });

        if (GameBuildType == MinecraftBuildTypeVersion.GDK) 
            await InstallWithGDK(dir, gameName);
        if (GameBuildType == MinecraftBuildTypeVersion.UWP)
            await InstallWithUWP(dir, gameName);
    }

    #region UWP Installer

    private async System.Threading.Tasks.Task InstallWithUWP(string dir, string gameName)
    {
        var path = Path.Combine(dir, "bedrock_versions", gameName);
        var manifest = PackageIdentity.ParseFromXml(ExtractAppxManifestFromAppx(PackFile));
        var gameType = GetVersionTypeWithUWP(manifest.Name);
        await GlobalModel.BedrockCore.InstallPackageAsync(new LocalGamePackageOptions()
        {
            GameName = gameName,
            Type = MinecraftBuildTypeVersion.UWP,
            InstallDstFolder = path,
            GameTypeVersion = gameType,
            FileFullPath = PackFile,
            ExtractionProgress = new Progress<DecompressProgress>((s) =>
            {
                ImportProgress.Report(new PackImportProgress()
                {
                    Progress = s.Percentage,
                    StatusMessage = $"解压文件中... ({s.Percentage:F2} %)"
                });
            }),
            DeployProgress = new Progress<DeploymentProgress>((s) =>
            {
                ImportProgress.Report(new PackImportProgress()
                {
                    Progress = s.percentage,
                    StatusMessage = $"部署游戏中 ({s.state.ToString()}) ({s.percentage:F2} %)"
                });
            })
        });
        
        GameInfoHelper.SaveVersionConfig(new VersionConfig()
        {
            Config = new VersionConfig.VersionConfigEntry(),
            Info = new VersionConfig.VersionInfo()
            {
                BuildType = MinecraftBuildTypeVersion.UWP,
                Version = manifest.Version,
                VersionName = gameName,
                VersionType = gameType
            },
            VersionPath = path
        });
    }
    
    private MinecraftGameTypeVersion GetVersionTypeWithUWP(string packName)
    {
        packName = packName.ToLower();
        if (packName.Contains("beta"))
            return MinecraftGameTypeVersion.Preview;
        return MinecraftGameTypeVersion.Release;
    }
    private string ExtractAppxManifestFromAppx(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
    
        // 查找AppxManifest.xml文件
        var manifestEntry = archive.Entries
            .FirstOrDefault(e => e.FullName.EndsWith("AppxManifest.xml", 
                StringComparison.OrdinalIgnoreCase));
    
        if (manifestEntry == null)
        {
            throw new FileNotFoundException("AppxManifest.xml not found in the archive");
        }
    
        // 读取到内存
        using var stream = manifestEntry.Open();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #endregion

    #region GDK Installer

    private async System.Threading.Tasks.Task InstallWithGDK(string dir, string gameName)
    {
        var path = Path.Combine(dir, "bedrock_versions", gameName);
        await GlobalModel.BedrockCore.InstallPackageAsync(new LocalGamePackageOptions()
        {
            GameName = gameName,
            Type = MinecraftBuildTypeVersion.GDK,
            InstallDstFolder = path,
            GameTypeVersion = MinecraftGameTypeVersion.Release,
            FileFullPath = PackFile,
            ExtractionProgress = new Progress<DecompressProgress>((s) =>
            {
                ImportProgress.Report(new PackImportProgress()
                {
                    Progress = s.Percentage,
                    StatusMessage = $"解压文件中... ({s.Percentage:F2} %)"
                });
            })
        });

        var manifest = PackageIdentity.ParseFromXml(File.ReadAllText(Path.Combine(path, "appxmanifest.xml")));
        
        GameInfoHelper.SaveVersionConfig(new VersionConfig()
        {
            Config = new VersionConfig.VersionConfigEntry(),
            Info = new VersionConfig.VersionInfo()
            {
                BuildType = MinecraftBuildTypeVersion.GDK,
                Version = manifest.Version,
                VersionName = gameName,
                VersionType = GetVersionTypeWithGDK(manifest.Name)
            },
            VersionPath = path
        });
    }
    private MinecraftGameTypeVersion GetVersionTypeWithGDK(string packName)
    {
        packName = packName.ToLower();
        if (packName.Contains("beta"))
            return MinecraftGameTypeVersion.Preview;
        return MinecraftGameTypeVersion.Release;
    }

    #endregion
}