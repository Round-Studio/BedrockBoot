using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Interface;
using BedrockBoot.Proton;
using BedrockBoot.Proton.Entry.Info;
using BedrockBoot.Views.Control.Items.Proton;
using BedrockBoot.Views.DialogContent.Linux.Proton;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.TaskItem.Linux.Proton;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;

public partial class GameProton : ISettingPage
{
    public static Action? UpdateList;
    public GameProton()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Game.Breadcrumb.Root"],
                ItemClickAction = s => MainSettingPage.NavigateTo(new SettingGame())
            },
            new()
            {
                ItemName = "Proton"
            }
        };

        UpdateList = UpdateUI;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if(ListBox == null) return;
        ListBox.Children.Clear();

        var lst = ProtonCore.GetInstalledVersions();
        if (lst != null)
        {
            lst.ToList().ForEach(l=>ListBox.Children.Add(new InstalledProtonItem(l, UpdateUI)));
        }
    }

    private void InstallProtonBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogChooseDownloadBranchContent();
        DialogHost.Show(new DialogInfo()
        {
            Title = "选择 GDKProton 分支",
            Content = dialog,
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var sleSource = dialog.SelSource;
                var selVersionDialog = new DialogChooseDownloadVersionContent(sleSource);
                
                DialogHost.Show(new DialogInfo()
                {
                    Title = "选择 GDKProton 版本",
                    Content = selVersionDialog,
                    CloseButtonText = "确定",
                    PrimaryButtonText = "取消",
                    AccountButton = DialogButtons.CloseButton,
                    CloseAction = () =>
                    {
                        var protonInfo = selVersionDialog.ProtonInfo;

                        TaskDownloadProtonItem.Install(protonInfo, new InstallInfo()
                        {
                            InstallName = protonInfo.Name,
                            IsOverWrite = true
                        });
                    }
                });
            }
        });
    }
}