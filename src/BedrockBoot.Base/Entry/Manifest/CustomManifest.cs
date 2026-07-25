using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Manifest;

public class CustomManifest
{
    [JsonPropertyName("format_version")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("randomStr")]
    public List<string> RandomStr { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("pageTitles")]
    public PageTitles PageTitles { get; set; }

    [JsonPropertyName("isShowHelpBtn")]
    public bool IsShowHelpBtn { get; set; }

    [JsonPropertyName("helpLinks")]
    public List<HelpLink> HelpLinks { get; set; }
}

public class PageTitles
{
    [JsonPropertyName("pageHome")]
    public string PageHome { get; set; }
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