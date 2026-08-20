using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;

public partial class GameBackup : ISettingPage
{
    public GameBackup()
    {
        InitializeComponent();

        BreadcrumbItem = new()
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Game.Breadcrumb.Root"],
                ItemClickAction = s => MainSettingPage.NavigateTo(new SettingGame())
            },
            new()
            {
                ItemName = "存档备份"
            }
        };
        
        UpdateUI();
    }

    public void UpdateUI()
    {
        IsEdit = false;
        this.ListBox.Children.Clear();
        var backups =
            GlobalModel.ArchiveBackup.IndexConfig.Data.Index.Select(x =>
                GlobalModel.ArchiveBackup.GetArchiveBackupsWhitUuid(x)).ToList();

        IsEdit = true;
    }
}