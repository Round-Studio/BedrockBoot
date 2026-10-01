using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.Models.Pack.Xbox.Cape;

public class CapeResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("totalCapes")] public int TotalCapes { get; set; }

    [JsonPropertyName("capes")] public List<Cape> Capes { get; set; } = new();
}

public class Cape
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";

    [JsonPropertyName("title")] public string Title { get; set; } = "";

    [JsonPropertyName("ownership")] public string Ownership { get; set; } = "";

    [JsonPropertyName("rarity")] public string Rarity { get; set; } = "";

    [JsonPropertyName("thumbnail")] public string Thumbnail { get; set; } = "";

    [JsonPropertyName("packUuid")] public string PackUuid { get; set; } = "";
}