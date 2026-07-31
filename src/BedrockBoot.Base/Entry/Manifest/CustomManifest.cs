using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Manifest;

public class CustomManifest
{
    [JsonPropertyName("format_version")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("randomStr")] public List<string> RandomStr { get; set; } = new();

    [JsonPropertyName("title")] public string Title { get; set; } = "BedrockBoot {{version}}";

    [JsonPropertyName("pageTitles")] public PageTitles PageTitles { get; set; } = new();

    [JsonPropertyName("isShowHelpBtn")] public bool IsShowHelpBtn { get; set; } = false;

    [JsonPropertyName("helpLinks")] public List<HelpLink> HelpLinks { get; set; } = new();
}

public class PageTitles
{
    [JsonPropertyName("pageHome")] public string PageHome { get; set; } = "您好，欢迎回来";
}

public class HelpLink
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; }
}