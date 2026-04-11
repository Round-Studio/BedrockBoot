using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
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

        IsConsoleModel.IsChecked = BedrockBoot.Core.Global.GlobalModel.Config.Data.IsConsole;
        IsEdit = true;
    }

    private void CheckBox_Change(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            BedrockBoot.Core.Global.GlobalModel.Config.Data.IsConsole = IsConsoleModel.IsChecked ?? false;

            BedrockBoot.Core.Global.GlobalModel.Config.Save();
        }
    }

    private void ExceptionBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new UniversalException());
    }
}