using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Integration;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.News;
using BedrockBoot.LeviLamina.Base.Entry.Porgress;
using BedrockBoot.LeviLamina.Models.Installer;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Integration;
using BedrockLauncher.Core;

public class Program
{
    private static async Task Main()
    {
        /*for (int i = 0; i < 100; i++)
        {
            NewsGenerate.GetRandomLine().ForEach(x=>Console.Write($"{x} "));
            Console.WriteLine();
        }*/

        NewsGenerate.GetPatchNotesAsync(SourceList.NewsUrl).Result.ForEach(x=>Console.WriteLine(x.Title));
    }
}