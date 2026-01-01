using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Mods;

public class ModInfo
{
    /// <summary>
    /// File 为模组文件
    /// </summary>
    [JsonPropertyName("file")]
    public string File { get; set; }

    /// <summary>
    /// InjectDelay 为注入延迟，单位 ms
    /// </summary>
    [JsonPropertyName("injectDelay")]
    public int InjectDelay { get; set; } = 0;
}