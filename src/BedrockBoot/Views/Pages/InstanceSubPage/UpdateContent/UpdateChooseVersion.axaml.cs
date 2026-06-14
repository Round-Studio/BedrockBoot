using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Pages.InstanceSubPage.UpdateContent;

public partial class UpdateChooseVersion : UserControl
{
    private readonly List<BuildInfo> _buildInfos;

    public UpdateChooseVersion()
    {
        InitializeComponent();
    }

    public UpdateChooseVersion(List<BuildInfo> buildInfos) : this()
    {
        _buildInfos = buildInfos;
        _buildInfos.Reverse();
        UpdateUi();
    }

    private void UpdateUi()
    {
        _buildInfos.ForEach(i =>
        {
            VersionsBox.Items.Add(new GameDownloadListBoxItem(i));
        });
        VersionsBox.SelectedIndex = 0;
    }
}