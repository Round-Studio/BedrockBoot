using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Interface;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

public partial class UniversalProjectManager : ISettingPage
{
    public UniversalProjectManager()
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
                ItemName = I18nManager.Instance["Setting.Universal.Debug.Title"],
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new UniversalDebug())
            },
            new()
            {
                ItemName = "项目管理"
            }
        };
    }

    private void CreatProject_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogCreatePluginProjectContent();
        DialogHost.Show(new()
        {
            Title = "创建插件项目",
            Content = dialog,
            CloseButtonText = "创建",
            PrimaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var packConf = dialog.PackConfig;
                DialogHost.Show(new()
                {
                    Content = new DialogCreatePluginProjectLoadingContent(packConf),
                    Title = "创建项目"
                });
            }
        });
    }
}