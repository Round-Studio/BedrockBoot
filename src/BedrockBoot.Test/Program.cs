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
        var body = new ResourcePackAnalysis(@"E:\Bedrock\WorldEdit_0.10.4.mcaddon");
        Console.WriteLine(body.GetPackType());
        body.GetPackManifests().ForEach(maf =>
        {
            Console.WriteLine($"{maf.Header.Name} - {maf.PackType}");
        });
    }
}