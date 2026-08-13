using System.Text.Json.Serialization;
using Round.SDK.Entry;

namespace BedrockBoot.Base.Entry.Info.Develop;

public class ProjectInfo
{
    [JsonPropertyName("projectName")] public string ProjectName { get; set; } = "";
    [JsonIgnore] public string ProjectPath { get; set; } = "";
    [JsonPropertyName("packInfo")] public PackConfig PackInfo { get; set; } = new PackConfig();
}