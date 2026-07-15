using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum.Type;

namespace BedrockBoot.Base.Entry.Config;

public class WidgetLayoutData
{
    [JsonPropertyName("widgetConfig")] public WidgetConfig WidgetConfig { get; set; } = new();
        
    [JsonPropertyName("gridX")]
    public int GridX { get; set; }
        
    [JsonPropertyName("gridY")]
    public int GridY { get; set; }
        
    [JsonPropertyName("size")]
    public WidgetSize Size { get; set; }
}

public class WidgetConfig
{
    [JsonPropertyName("widgetType")]
    public WidgetType WidgetType { get; set; } =  WidgetType.Timer;
}