using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum;

namespace BedrockBoot.Base.Entry.Pack.Theme
{
    public class ThemePackManifest
    {
        [JsonIgnore] public string? PackHash { get; set; } = string.Empty;
        [JsonIgnore] public bool IsSelectThis { get; set; } = false;
    
        [JsonPropertyName("formatVersion")] public int FormatVersion { get; set; } = 1;
        [JsonPropertyName("packName")] public string? PackName { get; set; } = "Unknown";
        [JsonPropertyName("packDescription")] public string? PackDescription { get; set; } = "Unknown";
        [JsonPropertyName("packAuthor")] public string? PackAuthor { get; set; } = "Unknown";
        [JsonPropertyName("packSupport")] public List<string>? PackSupport { get; set; } = new() { "BedrockBoot" };
    
        [JsonPropertyName("themeType")] public ThemeModelEnum ThemeType { get; set; } = ThemeModelEnum.Dark;
        [JsonPropertyName("themeColorCode")] public string? ThemeColor { get; set; } = string.Empty;
    
        [JsonPropertyName("backgroundUse3D")] public bool BackgroundUse3D { get; set; } = false;
        [JsonPropertyName("backgroundImageOpacity")] public int BackgroundImageOpacity { get; set; } = 100;
        [JsonPropertyName("backgroundImageBlur")] public int BackgroundImageBlur { get; set; } = 1;
        [JsonPropertyName("backgroundAnimation")] public bool BackgroundAnimation { get; set; } = false;
    
        [JsonPropertyName("backgroundImageFileName")] public string? BackgroundImageFileName { get; set; } = string.Empty;
        [JsonPropertyName("backgroundMusicFileName")] public string? BackgroundMusicFileName { get; set; } = string.Empty;
        [JsonPropertyName("packIconFileName")] public string? PackIconFileName { get; set; } = string.Empty;
    }
}