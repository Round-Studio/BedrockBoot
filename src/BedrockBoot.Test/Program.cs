using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Integration;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.LeviLamina.Base.Entry.Porgress;
using BedrockBoot.LeviLamina.Models.Installer;
using BedrockBoot.Models.Pack.Game.Integration;
using BedrockLauncher.Core;

public class Program
{
    private static async Task Main()
    {
        var llInstaller = new LeviLaminaInstaller(new VersionConfig()
        {
            VersionPath = @"D:\BedrockBoot\bedrock_versions\1.21.13101"
        });
        llInstaller.Progress = new Progress<InstallerProgress>((p) =>
        {
            Console.WriteLine($"{p.Message} - {p.Status} - {p.Progress:F2} %");
        });
        await llInstaller.InstallLeviLamina("1.9.4");
    }
}