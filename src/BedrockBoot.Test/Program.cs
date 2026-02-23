using BedrockBoot.Chunker;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Chunker.Event;
using BedrockBoot.Chunker.Jvm;

public class Program
{
    private static async Task Main()
    {
        var chunker = new Chunker()
        {
            JvmInfo = JavaUtil.GetJavaListAsync().Result.First(),
            JavaEditionVersion = "1.21.11"
        };

        chunker.BeginChunker(ChunkerType.BedrockToJava, "E:\\testWorld",
            "D:\\BedrockBoot\\bedrock_versions\\1.26.2\\config\\BedrockBoot2\\isolation\\Users\\2818413420751248947\\games\\com.mojang\\minecraftWorlds\\BobjNnseFv0=",
            new Progress<double>(p =>
            {
                Console.WriteLine($"进度：{p:F2} %");
            }));
    }
}