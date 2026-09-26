using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using BedrockBoot.Interface;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Items;

namespace BedrockBoot.Views.Pages.OtherPage.BuildRelease;

public partial class ChangLogPage : ISettingPage
{
    public ChangLogPage()
    {
        InitializeComponent();

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Universal.Breadcrumb.Root"],
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new SettingUniversal())
            },
            new()
            {
                ItemName = I18nManager.Instance["Setting.Universal.SoftwareUpdate.Title"],
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new UniversalSoftwareUpdate())
            },
            new()
            {
                ItemName = "修改日志"
            }
        };
        BuildLogList();
    }

    private void BuildLogList()
    {
        var list = this.FindControl<StackPanel>("LogList");
        var hint = this.FindControl<TextBlock>("EmptyHint");
        if (list == null) return;

        var entries = ChangeLogLoader.Load();

        if (entries.Count == 0)
        {
            if (hint != null) hint.IsVisible = true;
            return;
        }

        if (hint != null) hint.IsVisible = false;

        foreach (var entry in entries)
        {
            list.Children.Add(CreateEntryCard(entry));
        }
    }

    private static Avalonia.Controls.Control CreateEntryCard(ChangeLogEntry entry)
    {
        var message = new TextBlock
        {
            Text = entry.Message,
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        };

        var author = new TextBlock
        {
            Text = entry.Author,
            FontSize = 12,
            Opacity = 0.75
        };

        var date = new TextBlock
        {
            Text = entry.DisplayDate,
            FontSize = 12,
            Opacity = 0.75,
            Margin = new Thickness(12, 0, 0, 0)
        };

        var meta = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0
        };
        meta.Children.Add(author);
        meta.Children.Add(date);

        var left = new StackPanel
        {
            Spacing = 6
        };
        left.Children.Add(message);
        left.Children.Add(meta);

        var hash = new TextBlock
        {
            Text = entry.ShortHash,
            FontSize = 12,
            FontFamily = new FontFamily("Consolas,Menlo,monospace"),
            Opacity = 0.55,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(12, 0, 0, 0)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        Grid.SetColumn(left, 0);
        Grid.SetColumn(hash, 1);
        grid.Children.Add(left);
        grid.Children.Add(hash);

        var border = new BorderCard()
        {
            Padding = new Thickness(14, 12, 14, 12),
            Content = grid
        };

        return border;
    }
}