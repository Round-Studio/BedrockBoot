using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum.Search;

namespace BedrockBoot.Base.Entry.Info;

public class SearchInfo
{
    [JsonPropertyName("type")]
    public SearchResourceType Type { get; set; }
    
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
}