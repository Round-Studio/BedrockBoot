using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Enum;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingGame : ISetting
{
    public SettingGame()
    {
        InitializeComponent();
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "游戏"
            }
        });
        IsolationTypeBox.SelectedIndex = (int)GlobalModel.Config.Data.IsolationModel;

        IsEdit = true;
    }

    private void IsolationTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsolationModel = (IsolationType)IsolationTypeBox.SelectedIndex;
            GlobalModel.Config.Save();
        }
    }

    private void GameFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigationFrame.NavigateTo(new GameFolders());
    }
}