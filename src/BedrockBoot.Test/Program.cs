using BedrockLauncher.Core;

public class Program
{
    static async Task Main()
    {
        var bedrockCore = new BedrockCore();
        bedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Release).Wait();
    }
}