using System.Text.Json.Serialization;

namespace BedrockBoot.Standard.Entity.Game.Pack.Capes.Manifest;

public class CapeIndex
{
    [JsonPropertyName("serialize_name")]
    public string SerializeName { get; set; } = "";

    [JsonPropertyName("localization_name")]
    public string LocalizationName { get; set; } = "";

    [JsonPropertyName("capes")]
    public List<CapeDefinition> Capes { get; set; } = new();
}

public class CapeDefinition
{
    [JsonPropertyName("localization_name")]
    public string LocalizationName { get; set; } = "";

    [JsonPropertyName("texture")]
    public string Texture { get; set; } = "";

    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }

    [JsonPropertyName("elytra")]
    public string? Elytra { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }
}