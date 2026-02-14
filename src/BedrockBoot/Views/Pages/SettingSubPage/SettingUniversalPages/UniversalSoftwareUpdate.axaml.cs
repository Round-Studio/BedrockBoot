using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BedrockBoot.Base.Enum;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

public partial class UniversalSoftwareUpdate : ISettingPage
{
    public UniversalSoftwareUpdate()
    {
        InitializeComponent();
        IsAutoCheckUpdate.IsChecked = GlobalModel.Config.Data.IsAutoCheckUpdate;
        UpdateTypeBox.SelectedIndex = (int)GlobalModel.Config.Data.UpdateType;
        VersionCard.Description = GlobalModel.BodyVersion;
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "通用",
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new SettingUniversal())
            },
            new()
            {
                ItemName = "软件更新"
            }
        };

        IsEdit = true;
    }

    private void IsAutoCheckUpdate_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsAutoCheckUpdate = (bool)IsAutoCheckUpdate.IsChecked;
            GlobalModel.Config.Save();
        }
    }

    private void UpdateTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.UpdateType = (UpdateType)UpdateTypeBox.SelectedIndex;
            GlobalModel.Config.Save();
        }
    }
    private async void CheckUpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        CheckUpdateBtn.IsEnabled = false;
        CheckUpdateBtn.Content = new ProgressRing
        {
            Width = 24,
            Height = 24,
            Background = Brushes.Transparent
        };
        await MainPage.Update(true);
        CheckUpdateBtn.IsEnabled = true;
        CheckUpdateBtn.Content = "检查更新";
    }
}