using BedrockBoot.Base.Entry.Game.Pack.Import;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Import;
using BedrockLauncher.Core;

public class Program
{
    static async Task Main()
    {
        GlobalModel.BedrockCore = new BedrockCore();
        var body = new PackInstaller(@"K:\Bedrock\Microsoft.MinecraftWindowsBeta_1.21.12020.0_x64__8wekyb3d8bbwe.Appx");
        body.ImportProgress = new Progress<PackImportProgress>((s) =>
        {
            Console.WriteLine($"{s.StatusMessage} - {s.Progress:F2} %");
        }); 

        // 使用 await 等待安装过程完成
        body.Install(@"K:\Bedrock", Guid.NewGuid().ToString().Replace("-", "")).Wait();
    
        // 安装完成后，可以添加以下提示，程序不会立即退出
        Console.WriteLine("安装完成，按任意键退出...");
        Console.ReadKey();
    }
}