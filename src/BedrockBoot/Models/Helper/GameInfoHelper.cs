using System.IO;
using System.Text.Json;
using BedrockBoot.Base.Entry.Game;

namespace BedrockBoot.Models.Helper;

public class GameInfoHelper
{
    public static VersionInfo GetVersionInfo(string gamePath)
    {
        var jsonFile = Path.Combine(gamePath,"version.json");
        if (!File.Exists(jsonFile))
            return null;

        var json = File.ReadAllText(jsonFile);
        return JsonSerializer.Deserialize<VersionInfo>(json);
    }
}