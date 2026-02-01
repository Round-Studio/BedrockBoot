using BedrockLauncher.Core;

public class Program
{
    private static async Task Main()
    {
        var bedrockCore = new BedrockCore();
        bedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Release).Wait();
    }
}