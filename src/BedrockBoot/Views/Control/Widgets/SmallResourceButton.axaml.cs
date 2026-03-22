using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BedrockBoot.Views.Control.Widgets;

public partial class SmallResourceButton : UserControl
{
    // 定义属性
    public static readonly StyledProperty<string> ResourceNameProperty =
        AvaloniaProperty.Register<SmallResourceButton, string>(nameof(ResourceName));

    public static readonly StyledProperty<string> AuthorProperty =
        AvaloniaProperty.Register<SmallResourceButton, string>(nameof(Author));

    public static readonly StyledProperty<string> IconUrlProperty =
        AvaloniaProperty.Register<SmallResourceButton, string>(nameof(IconUrl));

    public string ResourceName { get => GetValue(ResourceNameProperty); set => SetValue(ResourceNameProperty, value); }
    public string Author { get => GetValue(AuthorProperty); set => SetValue(AuthorProperty, value); }
    public string IconUrl { get => GetValue(IconUrlProperty); set => SetValue(IconUrlProperty, value); }

    // 定义向外暴露的 Click 事件
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<SmallResourceButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    public SmallResourceButton()
    {
        InitializeComponent();
    }

    // 内部 Button 点击时，触发 UserControl 的 Click 事件
    private void OnInternalButtonClick(object? sender, RoutedEventArgs e)
    {
        var args = new RoutedEventArgs(ClickEvent);
        RaiseEvent(args);
    }
}