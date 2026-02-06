using System.Text.Json.Serialization;
using BedrockBoot.LevelNbt.Base.Entry;
using Round.SDK.Helper;

namespace BedrockBoot.Base.Entry.Game.Pack.Archive;

public class ArchiveInfo
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("path")] public string Path { get; set; }
    [JsonPropertyName("iconPath")] public string IconPath { get; set; }
    [JsonPropertyName("isProject")] public bool IsProject { get; set; } = false;
    [JsonPropertyName("levelWorldData")] public LevelWorldData LevelWorldData { get; set; }

    public void Save(string saveFile)
    {
        ZipHelper.CreateZipFile(Path, saveFile);
    }
}