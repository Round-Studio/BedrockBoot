using System.Collections.Generic;
using System.Windows.Documents;
using Avalonia.Controls;
using BedrockBoot.Base.Enum.Type;

namespace BedrockBoot.Interface;

public class IWidgetTemplated : UserControl
{
    public List<WidgetSize> SupportWidgetSize { get; set; } = new()
    {
        WidgetSize.ExtraLarge,
        WidgetSize.Large,
        WidgetSize.Medium,
        WidgetSize.Small
    };
}