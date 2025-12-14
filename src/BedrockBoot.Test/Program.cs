using BedrockBoot.Base.Entry.Game.Pack.Import;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Models.Pack.Game.Import;
using BedrockLauncher.Core;

public class Program
{
    static async Task Main()
    {
        ArchiveSerializer nbt = new ArchiveSerializer(@"C:\Users\ahadd\AppData\Roaming\Minecraft Bedrock\Users\2818413420751248947\games\com.mojang\minecraftWorlds\ZPm5oHr3PJs=");
        nbt.LoadInfo();
        
        Console.WriteLine("测试完成，按任意键退出...");
        Console.ReadKey();
    }
}