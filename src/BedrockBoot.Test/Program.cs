using BedrockBoot.Chunker;
using BedrockBoot.Chunker.Base.Entry;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Chunker.Event;
using BedrockBoot.Chunker.Jvm;
using BedrockBoot.Models.Pack.Chunker;

public class Program
{
    private static async Task Main()
    {
        if (!Chunker.CheckChunker())
            await Chunker.DownloadChunker(DownloadType.Github, new Progress<DownloadProgressEventArgs>(pro =>
            {
                Console.WriteLine(pro.Percentage);
            }));
        
        new ChunkerHelper(
            ChunkerType.BedrockToJava,
            "1.21.0", 
            "J://test.mcworld", 
            JavaUtil.GetJavaListAsync().Result.First(),
            new Progress<double>(p =>
            {
                Console.WriteLine($"进度：{p:F2} %");
            }))
            .ConversionToFolder("G:\\Minecraft\\.minecraft\\versions\\1.21.11-Fabric_0.18.4\\saves\\新的世界1");
    }
}