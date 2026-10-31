using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace BedrockBoot.Style.Widgets;

public class TopBarLabel : TemplatedControl
{
    public string TitleText
    {
        get => GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<string> TitleTextProperty =
        AvaloniaProperty.Register<TopBarLabel, string>(nameof(TitleText));

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<TopBarLabel, string>(nameof(Text));
}