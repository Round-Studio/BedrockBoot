using System.Collections.Generic;
using System.IO;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Isolation;

namespace BedrockBoot.Models.Pack.Game.Options;

public class GameOptionsManager(VersionConfig config)
{
    public string[] GetGameOptions(string user = "Shared")
    {
        var optionPath =
            Path.Combine(IsolationCore.GetInstanceFolderPath(config, InstanceFolderType.OptionFolder, user),
                "options.txt");
        
        if (!File.Exists(optionPath))
            return null;

        return File.ReadAllLines(optionPath);
    }

    public void SaveGameOptions(string[] gameOptions, string user = "Shared")
    {
        var optionPath =
            Path.Combine(IsolationCore.GetInstanceFolderPath(config, InstanceFolderType.OptionFolder, user),
                "options.txt");

        if (!File.Exists(optionPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(optionPath)!);
        }

        File.WriteAllLines(optionPath, gameOptions);
    }

    public List<string> GetUsers() => IsolationCore.GetInstanceUsers(config);
}