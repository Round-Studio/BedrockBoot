using System;
using System.Diagnostics;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Windows.SubWindows;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainToolsBoxPage : BedrockBootPage
{
    private static I18nManager i18n => I18nManager.Instance;

    public MainToolsBoxPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 修复丢失的游戏文件
    /// </summary>
    private void FoundLoseFilesBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var rfw = new RecoverFilesWindow();
        rfw.ShowDialog(GlobalModel.MainWindow);
    }

    /// <summary>
    /// 卸载所有已安装的 UWP 游戏组件
    /// </summary>
    private async void DeleteMcBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // 显示正在卸载的进度提示
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["MainPage.Tools.Uninstall.Dialog.Title"],
            Content = i18n["MainPage.Tools.Uninstall.Dialog.Content"]
        });

        try
        {
            // 依次移除不同版本的 UWP 实例
            await GlobalModel.BedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Release);
            await GlobalModel.BedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Preview);
            await GlobalModel.BedrockCore.RemoveUWPGameAsync(MinecraftGameTypeVersion.Beta);
        }
        catch (Exception ex)
        {
            // 如果卸载过程中出现异常，可以在此处捕获并记录
            Console.WriteLine($@"Uninstall failed: {ex.Message}");
        }
        finally
        {
            // 无论成功与否，任务结束后关闭对话框
            DialogHost.Close();
        }
    }
}