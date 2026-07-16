using BedrockBoot.Base.Enum.Type;

namespace BedrockBoot.Base.Entry.Info;

public class WidgetRegisterInfo
{
    public string Name { get; set; } = "New Widget";
    public string Description { get; set; } = "这是一个小组件";
    public Type? WidgetTypeof { get; set; }
    public WidgetType Type { get; set; }
    public WidgetSize DefaultSize { get; set; } = WidgetSize.Small;
}