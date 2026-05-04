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
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Notice;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;

public partial class GameProton : ISettingPage
{
    public static Action? UpdateList;
    
    private ComboBox? _globalProtonComboBox;
    private InfoCard? _emptyInfoCard;
    
    public GameProton()
    {
        InitializeComponent();
        
        _globalProtonComboBox = this.FindControl<ComboBox>("GlobalProtonComboBox");
        _emptyInfoCard = this.FindControl<InfoCard>("EmptyInfoCard");
        
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
        
        IsEdit = false;
        ListBox.Children.Clear();

        var lst = ProtonCore.GetInstalledVersions();
        
        if (lst != null && lst.Any())
        {
            // 有已安装版本：隐藏空提示框，显示列表
            if (_emptyInfoCard != null)
                _emptyInfoCard.IsVisible = false;
            
            lst.ToList().ForEach(l => ListBox.Children.Add(new InstalledProtonItem(l, UpdateUI)));
        }
        else
        {
            // 无已安装版本：显示空提示框，隐藏列表（或者列表本来就是空的）
            if (_emptyInfoCard != null)
                _emptyInfoCard.IsVisible = true;
        }
        
        // 刷新 ComboBox
        RefreshComboBox();

        IsEdit = true;
    }
    
    private void RefreshComboBox()
    {
        if (_globalProtonComboBox == null) return;
        
        var installedVersions = ProtonCore.GetInstalledVersions();
        
        _globalProtonComboBox.Items.Clear();
        
        if (installedVersions != null && installedVersions.Any())
        {
            _globalProtonComboBox.IsEnabled = true;
            foreach (var version in installedVersions)
            {
                _globalProtonComboBox.Items.Add(version);
            }
            
            _globalProtonComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("Name");
            var index = installedVersions.ToList()
                .FindIndex(x => x.InstallPath == ProtonCore.Config.Data.SelectProtonPath);
            _globalProtonComboBox.SelectedIndex = index;
        }
        else
        {
            _globalProtonComboBox.IsEnabled = false;
            _globalProtonComboBox.Items.Add(I18nManager.Instance["Settings.Game.Proton.Empty.Combo"]);
            _globalProtonComboBox.SelectedIndex = 0;
            
            ProtonCore.Config.Data.SelectProtonPath = string.Empty;
            ProtonCore.Config.Save();
        }
    }

    private void InstallProtonBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogChooseDownloadBranchContent();
        DialogHost.Show(new DialogInfo()
        {
            Title = I18nManager.Instance["Settings.Game.Proton.Dialog.Branch.Title"],
            Content = dialog,
            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var sleSource = dialog.SelSource;
                var selVersionDialog = new DialogChooseDownloadVersionContent(sleSource);
                
                DialogHost.Show(new DialogInfo()
                {
                    Title = I18nManager.Instance["Settings.Game.Proton.Dialog.Version.Title"],
                    Content = selVersionDialog,
                    CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
                    PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
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

    private void GlobalProtonComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            var installedVersions = ProtonCore.GetInstalledVersions();
            var index = GlobalProtonComboBox.SelectedIndex;
            var info = installedVersions?[index];
            
            ProtonCore.Config.Data.SelectProtonPath = info?.InstallPath!;
            ProtonCore.Config.Save();
        }
    }
}