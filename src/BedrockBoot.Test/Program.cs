using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Integration;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Models.Pack.Game.Integration;
using BedrockLauncher.Core;

public class Program
{
    private static async Task Main()
    {
        /*var packager = new IntegrationPackager(new VersionConfig()
        {
            VersionPath = @"E:\Bedrock\bedrock_versions\1.21.13201",
            Info = new VersionConfig.VersionInfo()
            {
                BuildType = MinecraftBuildTypeVersion.GDK
            }
        })
        {
            IntegrationProgress = new Progress<IntegrationProgress>((p =>
            {
                Console.WriteLine($@"{p.Message} - {p.Progress:F2} %");
            }))
        };
        packager.BeginPack(new PackInfo()
        {
            PackSavePath = $@"E:/testPack.mcpint",
        });*/

        var packInstall = new IntegrationInstaller($@"E:\测试整合包.mcpint");
        packInstall.IntegrationProgress = new Progress<InstallIntegrationProgress>((s) =>
        {
            Console.WriteLine($"{s.Message} - {s.Status} - {s.Progress:F2}");
        });
        await packInstall.BeginInstaller($"D:\\BedrockBoot","测试整合包安装12");

        Console.ReadKey();
    }
}