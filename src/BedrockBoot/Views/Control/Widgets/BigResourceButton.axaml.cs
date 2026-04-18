using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Control.Widgets;

public partial class BigResourceButton : UserControl
{
    // 定义点击事件路由
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<BigResourceButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public BigResourceButton()
    {
        InitializeComponent();
        MainButton.Click += (s, e) => RaiseEvent(new RoutedEventArgs(ClickEvent));
    }

    public string ResourceName
    {
        get => ResourceNameText.Text ?? "";
        set => ResourceNameText.Text = value;
    }

    public string Description
    {
        get => DescriptionText.Text ?? "";
        set => DescriptionText.Text = value;
    }

    public string DownloadCount
    {
        get => DownloadCountText.Text ?? "";
        set => DownloadCountText.Text = value;
    }

    public string UpdateDate
    {
        get => UpdateDateText.Text ?? "";
        set => UpdateDateText.Text = value;
    }

    public string Author
    {
        get => AuthorText.Text ?? "";
        set => AuthorText.Text = value;
    }

    public IEnumerable<string>? Labels
    {
        set => UpdateLabels(value.ToList());
    }

    public string? IconUrl
    {
        get => CoverImage.ImageUrl;
        set => CoverImage.ImageUrl = value;
    }

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    private void UpdateLabels(List<string> labels)
    {
        labels.ForEach(x => LabelsControl.Children.Add(new LabelBox { Text = x }));
    }
}