using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace BedrockBoot.Views.Control.Widgets;

public partial class SmallGameView : UserControl
{
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<SmallGameView, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public SmallGameView()
    {
        InitializeComponent();
        MainButton.Click += (s, e) => RaiseEvent(new RoutedEventArgs(ClickEvent));
    }

    public string Version
    {
        get => VersionText.Text ?? "";
        set => VersionText.Text = value;
    }

    public string Description
    {
        get => DescriptionText.Text ?? "";
        set => DescriptionText.Text = value;
    }

    public IImage Icon
    {
        get => IconImage.Source;
        set => IconImage.Source = value;
    }

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }
}