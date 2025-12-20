using System;
using System.IO;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.OtherPage;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Service.Protocol;

public class ProtocolCommand
{
    public static void OnCommand(string[] command)
    {
        if (command.Contains("about"))
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                MainPage.Instance.SelTag.SelectedIndex = 5;
                MainSettingPage.NavigationFrame.NavigateTo(new AboutPage());
            });
        }
    }
}