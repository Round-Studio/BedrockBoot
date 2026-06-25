using Avalonia.Interactivity;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using System.Collections.Generic;



namespace BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;

public partial class MouseLock : ISettingPage
{
    public bool IsEdit;

    public MouseLock()
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
                ItemName = I18nManager.Instance["Settings.Game.MouseLock.Title"]
            }
        };
        MouseLockMainSwitch.IsChecked = GlobalModel.Config.Data.IsMouseLock;
        MouseLockForGdkSwitch.IsChecked = GlobalModel.Config.Data.IsMouseLockForGdk;
        MouseLockReserveSwitch.IsChecked = GlobalModel.Config.Data.IsMouseLockReserve;
        MouseLockHotkeyBtn.Content = GlobalModel.Config.Data.MouseLockHotkey;
        UpdateUI();

        // 理论上讲 Linux 版本应该进不来这个页面吧（
        IsEdit = true;

    }

    

    private void UpdateUI()
    {
        if ((bool)MouseLockMainSwitch.IsChecked!)
        {
            MouseLockMainCard.Glyph = "\uF19F";
            MouseLockForGdkCard.IsEnabled = true;
            MouseLockReserveCard.IsEnabled = true;
            MouseLockHotkeyCard.IsEnabled = true;
        }
        else
        {
            MouseLockMainCard.Glyph = "\uF19E";
            MouseLockForGdkCard.IsEnabled = false;
            MouseLockReserveCard.IsEnabled = false;
            MouseLockHotkeyCard.IsEnabled = false;
        }
    }

    private void MouseLockMainSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        UpdateUI();
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsMouseLock = (bool)MouseLockMainSwitch.IsChecked!;
            GlobalModel.Config.Save();
        }
    }
    private void MouseLockForGdkSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsMouseLockForGdk = (bool)MouseLockForGdkSwitch.IsChecked!;
            GlobalModel.Config.Save();
        }
    }
    private void MouseLockReserveSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            if((bool)MouseLockReserveSwitch.IsChecked!)
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = I18nManager.Instance["MainWindow.Dialog.Warning.Title"],
                    Content = I18nManager.Instance["Settings.Game.MouseLock.Reserve.Warn"],
                    PrimaryButtonText = I18nManager.Instance["Shared.Action.Confirm"],
                    CloseButtonText = I18nManager.Instance["Shared.Action.Cancel"],
                    CloseAction = () =>
                    {
                        MouseLockReserveSwitch.IsChecked = false;
                    },
                    PrimaryAction = () =>
                    {
                        GlobalModel.Config.Data.IsMouseLockReserve = true;
                        GlobalModel.Config.Save();
                    }
                });
            }
            else
            {
                GlobalModel.Config.Data.IsMouseLockReserve = false;
                GlobalModel.Config.Save();
            }

            
            
        }
    }
   
    private async void MouseLockHotkeyBtn_OnCick(object? sender, RoutedEventArgs e)
    {
#if WINDOWS
        if (IsEdit)
        {
            var oldContent = MouseLockHotkeyBtn.Content;

            MouseLockHotkeyBtn.Content = I18nManager.Instance["Settings.Game.MouseLock.Hotkey.Capturing"];

            var session = HotKeyHelper.Begin(MouseLockHotkeyBtn);

            var hotkey = await session.Task;

            if (hotkey == null)
            {
                MouseLockHotkeyBtn.Content = oldContent;
                return;
            }

            MouseLockHotkeyBtn.Content = hotkey.ToString();

            GlobalModel.Config.Data.MouseLockHotkey = hotkey.ToString();
            GlobalModel.Config.Save();
        }
#endif

    }
}