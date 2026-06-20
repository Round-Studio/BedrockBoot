using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DrawContent;
using IWshRuntimeLibrary;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Plugin.BedrockBoot.Register;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceControls : ISetting
{
    public InstanceControls()
    {
        IsEdit = false;
        InitializeComponent();

#if LINUX
        JumpItemBtn.IsVisible = false;
#endif

        Expansion.IsVisible = RegisterService.API.InstanceControlItems.Count > 0;
        RegisterService.API.InstanceControlItems.ForEach(it =>
        {
            var item = new SettingCard
            {
                Header = it.Header,
                Description = it.Description,
                Glyph = it.ItemGlyph,
                IsClickable = true
            };
            item.Click += (sender, args) => it.Callback?.Invoke(VersionInfo?.VersionPath!);
            ExpansionPanel.Children.Add(item);
        });
    }

    public InstanceControls(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
    }

    private static I18nManager i18n => I18nManager.Instance;

    public VersionConfig VersionInfo { get; set; }

    /// <summary>
    ///     删除实例逻辑
    /// </summary>
    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Control.Delete.Confirm.Title"],
            Content = string.Format(i18n["Instance.Control.Delete.Confirm.Content"],
                VersionInfo.Info.VersionName, VersionInfo.Info.Version),
            CloseButtonText = i18n["MainWindow.Common.Confirm"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = () =>
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = string.Format(i18n["Instance.Control.Delete.Process.Title"], VersionInfo.Info.VersionName),
                    Content = new DialogDeleteGameContent(VersionInfo)
                });
            },
            AccountButton = DialogButtons.PrimaryButton
        });
    }

    /// <summary>
    ///     创建桌面快捷方式
    /// </summary>
    private async void JumpItemBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = i18n["Instance.Control.Shortcut.Save.Title"],
            SuggestedFileName = $"{i18n["Instance.Control.Shortcut.Prefix"]} {VersionInfo.Info.VersionName}",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(i18n["Instance.Control.Shortcut.FileType"])
                {
                    Patterns = new[] { "*.lnk" }
                }
            }
        });

        if (file is not null)
            try
            {
                var shortcutPath = file.TryGetLocalPath();
                if (string.IsNullOrEmpty(shortcutPath)) return;

                if (!shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                    shortcutPath = Path.ChangeExtension(shortcutPath, ".lnk");

                var targetPath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                var arguments = $"-jump \"{VersionInfo.VersionPath}\"";

                var success = CreateShortcutInSTAThread(shortcutPath, targetPath, arguments);

                if (success)
                    DialogHost.Show(new DialogInfo
                    {
                        Title = i18n["Instance.Control.Shortcut.Success.Title"],
                        Content = string.Format(i18n["Instance.Control.Shortcut.Success.Content"],
                            Path.GetFileName(shortcutPath), Path.GetDirectoryName(shortcutPath)),
                        CloseButtonText = i18n["MainWindow.Common.Confirm"],
                        PrimaryButtonText = i18n["Instance.Control.Shortcut.Action.OpenFolder"],
                        PrimaryAction = () =>
                        {
                            try
                            {
                                Process.Start("explorer.exe", $"/select,\"{shortcutPath}\"");
                            }
                            catch
                            {
                                /* Ignore */
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = i18n["Instance.Control.Shortcut.Failed.Title"],
                    Content = $"{i18n["Instance.Control.Shortcut.Failed.Content"]}\n\n{ex.Message}",
                    CloseButtonText = i18n["MainWindow.Common.Confirm"]
                });
            }
    }

    private bool CreateShortcutInSTAThread(string shortcutPath, string targetPath, string arguments)
    {
        var success = false;
        Exception? threadException = null;

        var thread = new Thread(() =>
        {
            try
            {
                var shell = new WshShell();
                var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = targetPath;
                shortcut.Arguments = arguments;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath) ?? string.Empty;
                shortcut.Description = $"BedrockBoot Quick Launch - {VersionInfo.Info.VersionName}";
                shortcut.IconLocation = $"{targetPath},{SourceList.MinecraftIconID}";

                shortcut.Save();
                success = true;
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null) throw threadException;
        return success;
    }

    /// <summary>
    ///     导入/迁移配置
    /// </summary>
    private void ImportConfig_OnClick(object? sender, RoutedEventArgs e)
    {
        var body = new DialogChooseGameContent();
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Control.Import.Choose.Title"],
            Content = body,
            CloseButtonText = i18n["MainWindow.Common.Confirm"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = () =>
            {
                var confBody = new DialogImportInstanceConfigContent();
                DialogHost.Show(new DialogInfo
                {
                    Title = i18n["Instance.Control.Import.Content.Title"],
                    Content = confBody,
                    CloseButtonText = i18n["Instance.Control.Import.Action.Start"],
                    PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
                    CloseAction = () =>
                    {
                        var conf = confBody.MigrationConfig;
                        conf.NewVersionConfig = VersionInfo;
                        conf.OldVersionConfig = body.VersionConfig;

                        DialogHost.Show(new DialogInfo
                        {
                            Title = i18n["Instance.Control.Import.Progress.Title"],
                            Content = new DialogMigrationGameConfigContent(conf)
                        });
                    }
                });
            }
        });
    }

    /// <summary>
    ///     导出整合包
    /// </summary>
    private void MakePack_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogMakeIntegrationPackConfigContent();

        DialogHost.Show(new DialogInfo
        {
            Content = dialog,
            Title = i18n["Instance.Control.Pack.Dialog.Title"],
            CloseButtonText = i18n["MainWindow.Common.Confirm"],
            SecondaryButtonText = i18n["MainWindow.Common.Cancel"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = async () =>
            {
                var config = dialog.PackConfig;
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = i18n["Instance.Control.Pack.Save.Title"],
                    DefaultExtension = "mcpint",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType(i18n["Instance.Control.Pack.FileType"])
                        {
                            Patterns = new[] { "*.mcpint" }
                        }
                    }
                });

                if (file != null)
                {
                    config.PackSavePath = file.TryGetLocalPath();
                    config.VersionConfig = VersionInfo;
                    DialogHost.Show(new DialogInfo
                    {
                        Title = i18n["Instance.Control.Pack.Progress.Title"],
                        Content = new DialogMakeIntegrationPackContent(config)
                    });
                }
            }
        });
    }

    private void UpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new()
        {
            Title = "升级提示",
            Content = new StringBuilder()
                .AppendLine("此升级为强制升级，确认升级后将无法取消！")
                .AppendLine("强制升级游戏版本可能导致部分资源包，地图等不兼容。")
                .AppendLine("请问您确定要升级吗？"),
            CloseButtonText = i18n["MainWindow.Common.Confirm"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = () =>
            {
                GlobalModel.MainWindow.OpenDraw(new DrawUpdateInstanceContent(VersionInfo),
                    $"升级实例向导 - {VersionInfo.Info.VersionName} ({VersionInfo.Info.Version})");
            }
        });
    }
}