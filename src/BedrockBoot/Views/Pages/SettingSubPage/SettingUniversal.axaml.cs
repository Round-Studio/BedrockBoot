using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Enum.Language;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingUniversal : ISettingPage
{
    public SettingUniversal()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "通用"
            }
        };

        TaskBarJumpItem.IsChecked = GlobalModel.Config.Data.IsTaskBarJumpItem;
        LanguageChoose.SelectedIndex = (int)GlobalModel.Config.Data.Language;

        IsEdit = true;
    }

    private void SoftwareUpdate_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new UniversalSoftwareUpdate());
    }

    private void DebugBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new UniversalDebug());
    }

    private void TaskBarJumpItem_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsTaskBarJumpItem = (bool)TaskBarJumpItem.IsChecked;
            GlobalModel.Config.Save();

            JumpListManager.ConfigureJumpList();
        }
    }

    private void LanguageChoose_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.Language = (LanguageEnum)LanguageChoose.SelectedIndex;
            GlobalModel.Config.Save();

            L10nManager.Instance.SystemLanguage(GlobalModel.Config.Data.Language);
        }
    }
}