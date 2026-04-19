using System.Collections.Generic;
using Avalonia.Interactivity;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

public partial class UniversalDebug : ISettingPage
{
    public bool IsEdit;

    public UniversalDebug()
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
                ItemName = I18nManager.Instance["Setting.Universal.Debug.Title"]
            }
        };

        IsConsoleModel.IsChecked = GlobalModel.Config.Data.IsConsole;
        IsEdit = true;
    }

    private void CheckBox_Change(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsConsole = IsConsoleModel.IsChecked ?? false;

            GlobalModel.Config.Save();
        }
    }

    private void ExceptionBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new UniversalException());
    }
}