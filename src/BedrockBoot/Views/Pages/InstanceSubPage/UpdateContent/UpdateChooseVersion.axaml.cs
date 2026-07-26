using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Views.Control.Items;
using BedrockLauncher.Core;

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

    public BuildInfo? SelectedBuildInfo { get; private set; }

    private void UpdateUi()
    {
        // 绑定数据源，由 ItemTemplate 按需实例化条目，保证虚拟化生效
        VersionsBox.ItemsSource = _buildInfos;

        if (_buildInfos.Count > 0)
        {
            VersionsBox.SelectedIndex = 0;
            SelectedBuildInfo = _buildInfos[0];
        }

        VersionsBox.SelectionChanged += (_, _) =>
        {
            if (VersionsBox.SelectedIndex >= 0 && VersionsBox.SelectedIndex < _buildInfos.Count)
                SelectedBuildInfo = _buildInfos[VersionsBox.SelectedIndex];
        };
    }
}