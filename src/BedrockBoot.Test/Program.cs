using BedrockBoot.Base.Entry.Game.Pack.Import;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Models.Pack.Game.Import;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockLauncher.Core;

public class Program
{
    static async Task Main()
    {
        var bedrockCore = new BedrockCore();
        bedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Release).Wait();
    }
}