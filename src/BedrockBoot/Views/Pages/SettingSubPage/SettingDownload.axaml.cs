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
    public bool IsEdit;

    public SettingDownload()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "下载"
            }
        };

        ChunkCountSlider.Value = GlobalModel.Config.Data.DownloadChunkCount;
        SourceList.VersionDataSources.ToList().ForEach(s => SourceBox.Items.Add(new ComboBoxItem
        {
            Content = s.Key
        }));
        SourceBox.SelectedIndex = GlobalModel.Config.Data.VersionSourceIndex;
        SourceList.CurseForgeSource.ToList().ForEach(s => CurseForgeSourceBox.Items.Add(new ComboBoxItem
        {
            Content = s.Key
        }));
        CurseForgeSourceBox.SelectedIndex = GlobalModel.Config.Data.CurseForgeSourceIndex;
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
            if ((int)ChunkCountSlider.Value != GlobalModel.Config.Data.DownloadChunkCount)
            {
                GlobalModel.Config.Data.DownloadChunkCount = (int)ChunkCountSlider.Value;
                GlobalModel.Config.Save();
            }
    }

    private void SourceBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.VersionSourceIndex = SourceBox.SelectedIndex;
            GlobalModel.Config.Save();
        }
    }

    private void CurseForgeSourceBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.CurseForgeSourceIndex = CurseForgeSourceBox.SelectedIndex;
            GlobalModel.Config.Save();
        }
    }
}