using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;
using BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingDownload : ISettingPage
{

    public SettingDownload()
    {
        InitializeComponent();
        
        // 面包屑导航国际化
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Download.Breadcrumb.Root"]
            }
        };

        // 加载下载分片数配置
        ChunkCountSlider.Value = BedrockBoot.Core.Global.GlobalModel.Config.Data.DownloadChunkCount;

        // 动态加载版本下载源 (保持数据源原名)
        SourceBox.Items.Clear();
        foreach (var s in SourceList.VersionDataSources)
        {
            SourceBox.Items.Add(new ComboBoxItem { Content = s.Key });
        }
        SourceBox.SelectedIndex = BedrockBoot.Core.Global.GlobalModel.Config.Data.VersionSourceIndex;

        // 动态加载 CurseForge 下载源
        CurseForgeSourceBox.Items.Clear();
        foreach (var s in SourceList.CurseForgeSource)
        {
            CurseForgeSourceBox.Items.Add(new ComboBoxItem { Content = s.Key });
        }
        CurseForgeSourceBox.SelectedIndex = BedrockBoot.Core.Global.GlobalModel.Config.Data.CurseForgeSourceIndex;

        IsEdit = true;
    }

    private void GameFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new GameFolders());
    }

    private void SoftwareUpdate_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new UniversalSoftwareUpdate());
    }

    private void ChunkCountSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (IsEdit)
        {
            int newValue = (int)ChunkCountSlider.Value;
            if (newValue != BedrockBoot.Core.Global.GlobalModel.Config.Data.DownloadChunkCount)
            {
                BedrockBoot.Core.Global.GlobalModel.Config.Data.DownloadChunkCount = newValue;
                BedrockBoot.Core.Global.GlobalModel.Config.Save();
            }
        }
    }

    private void SourceBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            BedrockBoot.Core.Global.GlobalModel.Config.Data.VersionSourceIndex = SourceBox.SelectedIndex;
            BedrockBoot.Core.Global.GlobalModel.Config.Save();
        }
    }

    private void CurseForgeSourceBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            BedrockBoot.Core.Global.GlobalModel.Config.Data.CurseForgeSourceIndex = CurseForgeSourceBox.SelectedIndex;
            BedrockBoot.Core.Global.GlobalModel.Config.Save();
        }
    }
}