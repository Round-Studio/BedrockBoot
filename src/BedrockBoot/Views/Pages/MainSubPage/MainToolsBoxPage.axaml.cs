using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Chunker.Jvm;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
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
        
#if RELEASE
        TranslateResourcePack.IsVisible = GlobalModel.FunctionOption.IsEnableToolsBoxUsingPackTranslate;
#endif
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
        void OpenDialog()
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
                    jvms.ForEach(j => Console.WriteLine($@"Find Jvm: {j.JavaPath}"));
                    var jvm = jvms.Find(j => j.MajorVersion >= 17);
                
                    Dispatcher.UIThread.Invoke(DialogHost.Close);

                    if (jvm == null)
                    {
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            DialogHost.Show(new()
                            {
                                Title = "Jvm 错误",
                                Content = "未找到合适的 Jvm 运行器",
                                CloseButtonText = "确定"
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

        if (!Chunker.Chunker.CheckChunker())
        {
            var dialog = new DialogDownloadChunkerContent();
            DialogHost.Show(new ()
            {
                Title = "下载依赖",
                Content = dialog
            });
            
            dialog.Download(OpenDialog);
        }
        else
        {
            OpenDialog();
        }
    }

    private async void TranslateResourcePack_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        var dialog = new OpenFileDialog
        {
            Title = "导入需要翻译的基岩版资源包",
            AllowMultiple = false,
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "基岩版资源包/行为包",
                    Extensions = new List<string> { "mcpack", "mcaddon" }
                }
            }
        };

        if (window == null) return;

        var result = await dialog.ShowAsync(window);

        if (result != null && result.Any())
        {
            var selectedFile = result.First();

            var saveFileDialog = new SaveFileDialog
            {
                Title = "保存为基岩版支持包文件",
                DefaultExtension = Path.GetExtension(selectedFile),
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter
                    {
                        Name = "Minecraft 基岩版支持文件",
                        Extensions = new List<string> { Path.GetExtension(selectedFile) }
                    }
                }
            };

            if (window == null) return;

            var showAsync = await saveFileDialog.ShowAsync(window);
        
            if (!string.IsNullOrWhiteSpace(showAsync))
            {
                var saveFile = showAsync;
                var inputFile = selectedFile;
                
                DialogHost.Show(new()
                {
                    Title = "翻译包",
                    Content = new DialogTranslateResourcePackContent(inputFile, saveFile)
                });
            }
        }
    }
}