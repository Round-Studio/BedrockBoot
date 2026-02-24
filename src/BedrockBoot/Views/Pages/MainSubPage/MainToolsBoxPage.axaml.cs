using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Chunker.Jvm;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent.Chunker;
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

    private void WorldShift_OnClick(object? sender, RoutedEventArgs e)
    {
        Task.Run(async () =>
        {
            if (Chunker.Chunker.DefaultJvmInfo == null)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Show(new()
                    {
                        Title = "存档转换",
                        Content = "正在获取适合的 Jvm 运行器..."
                    });
                });

                var jvms = await JavaUtil.GetJavaListAsync();
                jvms.ForEach(j => Console.WriteLine($"Find Jvm: {j.JavaPath}"));
                var jvm = jvms.Find(j => j.MajorVersion >= 17);
                
                Dispatcher.UIThread.Invoke(DialogHost.Close);

                if (jvm == null)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        DialogHost.Show(new()
                        {
                            Title = "Jvm 错误",
                            Content = "未找到合适的 Jvm 运行器"
                        });
                    });
                    return;
                }
                
                Chunker.Chunker.DefaultJvmInfo = jvm;
            }
            
            Dispatcher.UIThread.Invoke(() =>
            {
                DialogHost.Show(new()
                {
                    Title = "存档转换",
                    Content = new DialogChooseChunkerTypeContent(),
                    CloseButtonText = "取消"
                });
            });
        });
    }
}