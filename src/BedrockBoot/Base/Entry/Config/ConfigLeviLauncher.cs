using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Config;

public class ConfigLeviLauncher
{
    [JsonPropertyName("base_root")] public string BaseRoot { get; set; }
}