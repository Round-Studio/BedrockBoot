using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Views.Pages.InstanceSubPage.DataStats;
using Path = System.IO.Path;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceInformation : UserControl
{
    private readonly VersionConfig _versionInfo;

    public InstanceInformation()
    {
        InitializeComponent();
    }

    public InstanceInformation(VersionConfig versionInfo) : this()
    {
        _versionInfo = versionInfo;
        NavigationFrame.NavigateTo(new PlayData(_versionInfo));
    }

    private void NavBar_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var tag = ((ListBoxItem)NavBar?.SelectedItem!).Tag?.ToString();
        object? page = null;
        if (tag != null)
            switch (tag)
            {
                case "PlayTime":
                    page = new PlayData(_versionInfo);
                    break;
                case "DiskUsage":
                    page = new PlayData(_versionInfo);
                    break;
            }

        if (page != null) NavigationFrame.NavigateTo(page);
    }
}