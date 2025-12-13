using System;
using System.IO;
using BedrockBoot.Base.Entry.Game.Pack.Import;
using BedrockBoot.Models.Global;
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
    }

    private async System.Threading.Tasks.Task InstallWithGDK(string dir, string gameName)
    {
        await GlobalModel.BedrockCore.InstallPackageAsync(new LocalGamePackageOptions()
        {
            GameName = gameName,
            Type = MinecraftBuildTypeVersion.GDK,
            InstallDstFolder = Path.Combine(dir, "bedrock_versions", gameName),
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
    }
}