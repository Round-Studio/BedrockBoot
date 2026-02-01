using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using IWshRuntimeLibrary;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using File = System.IO.File;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceControls : ISetting
{
    public InstanceControls()
    {
        IsEdit = false;
        InitializeComponent();

#if RELEASE
        MouseLock.IsVisible = GlobalModel.FunctionOption.IsEnableMouseLock;
#endif
    }

    public InstanceControls(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
    }

    public VersionConfig VersionInfo { get; set; }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = "确认删除",
            Content = $"您确定要删除 {VersionInfo.Info.VersionName} ({VersionInfo.Info.Version}) 吗，\n" +
                      $"这将永远无法恢复.jpg",
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = $"删除 {VersionInfo.Info.VersionName}",
                    Content = new DialogDeleteGameContent(VersionInfo)
                });
            },
            AccountButton = DialogButtons.PrimaryButton
        });
    }

    private async void JumpItemBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存快捷启动",
            SuggestedFileName = $"快捷启动 {VersionInfo.Info.VersionName}",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Windows 快捷方式 (.lnk)")
                {
                    Patterns = new[] { "*.lnk" }
                }
            }
        });

        if (file is not null)
        {
            try
            {
                var shortcutPath = file.TryGetLocalPath();

                // 确保路径是 .lnk 扩展名
                if (!shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                    shortcutPath = Path.ChangeExtension(shortcutPath, ".lnk");

                var targetPath = Process.GetCurrentProcess().MainModule.FileName;
                var arguments = $"-jump \"{VersionInfo.VersionPath}\"";

                // 创建快捷方式
                var success = CreateShortcutInSTAThread(shortcutPath, targetPath, arguments);

                if (success)
                {
                    DialogHost.Show(new DialogInfo
                    {
                        Title = "生成成功",
                        Content = $"快捷方式已成功创建！\n\n" +
                                  $"名称：{Path.GetFileName(shortcutPath)}\n" +
                                  $"位置：{Path.GetDirectoryName(shortcutPath)}",
                        CloseButtonText = "确定",
                        PrimaryButtonText = "打开所在文件夹",
                        PrimaryAction = () =>
                        {
                            // 打开快捷方式所在文件夹
                            try
                            {
                                Process.Start("explorer.exe", $"/select,\"{shortcutPath}\"");
                            }
                            catch
                            {
                                // 忽略错误
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = "创建失败",
                    Content = $"创建快捷方式失败：\n\n{ex.Message}",
                    CloseButtonText = "确定"
                });
            }
        }
    }

    private bool CreateShortcutInSTAThread(string shortcutPath, string targetPath, string arguments)
    {
        var success = false;
        Exception threadException = null;

        var thread = new Thread(() =>
        {
            try
            {
                var shell = new WshShell();
                var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = targetPath;
                shortcut.Arguments = arguments;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath) ?? string.Empty;
                shortcut.Description = $"BedrockBoot 快捷启动 - {VersionInfo.Info.VersionName}";
                shortcut.IconLocation = $"{targetPath},{SourceList.MinecraftIconID}";

                shortcut.Save();
                success = true;
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        // 设置为 STA 线程（COM 需要）
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
            throw threadException;

        return success;
    }

    private void ImportConfig_OnClick(object? sender, RoutedEventArgs e)
    {
        var body = new DialogChooseGameContent();
        DialogHost.Show(new DialogInfo()
        {
            Title = "选择需要导入的实例",
            Content = body,
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                var confBody = new DialogImportInstanceConfigContent();
                DialogHost.Show(new DialogInfo()
                {
                    Title = "选择导入内容",
                    Content = confBody,
                    CloseButtonText = "开始导入",
                    PrimaryButtonText = "取消",
                    CloseAction = () =>
                    {
                        var conf = confBody.MigrationConfig;
                        conf.NewVersionConfig = VersionInfo;
                        conf.OldVersionConfig = body.VersionConfig;
                        
                        DialogHost.Show(new DialogInfo()
                        {
                            Title = "迁移资源...",
                            Content = new DialogMigrationGameConfigContent(conf)
                        });
                    }
                });
            }
        });
    }
}