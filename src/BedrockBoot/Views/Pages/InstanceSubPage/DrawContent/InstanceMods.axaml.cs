using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Mods;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Path = System.IO.Path;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceMods : ISetting
{
    private static I18nManager i18n => I18nManager.Instance;

    public InstanceMods()
    {
        IsEdit = false;
        InitializeComponent();
    }

    public InstanceMods(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        ModsManager = new ModsManager(VersionInfo)
        {
            RefreshCallBack = UpdateUI
        };

        UpdateUI();
    }

    public VersionConfig VersionInfo { get; set; }
    public ModsManager ModsManager { get; set; }
    private string SearchKey => SearchBox.Text ?? string.Empty;

    /// <summary>
    /// 更新模组列表 UI
    /// </summary>
    private void UpdateUI()
    {
        IsEdit = false;
        NullBox.IsVisible = false;
        ResultBox.Children.Clear();

        var mods = ModsManager.RefreshMods();
        var resultMods = new List<ModInfo>();

        foreach (var info in mods)
        {
            // 使用不区分大小写的包含匹配
            if (string.IsNullOrEmpty(SearchKey) ||
                info.File.Contains(SearchKey, StringComparison.OrdinalIgnoreCase))
            {
                resultMods.Add(info);
            }
        }

        if (resultMods.Count <= 0)
        {
            NullBox.IsVisible = true;
        }
        else
        {
            foreach (var info in resultMods)
            {
                ResultBox.Children.Add(new GameModItem(info, VersionInfo)
                {
                    ModsManager = ModsManager,
                    UpdateCallBack = UpdateUI
                });
            }
        }

        IsEdit = true;
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit)
            UpdateUI();
    }

    /// <summary>
    /// 打开模组所在的物理文件夹
    /// </summary>
    private void FolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var modPath = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods");
        if (!Directory.Exists(modPath))
        {
            Directory.CreateDirectory(modPath);
        }
        Process.Start("explorer", new[] { modPath });
    }

    /// <summary>
    /// 导入模组文件
    /// </summary>
    private void ImportModBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogImportModContent();
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Mods.Import.Title"],
            Content = dialog,
            CloseButtonText = i18n["MainWindow.Common.Confirm"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = () =>
            {
                if (string.IsNullOrEmpty(dialog.ModFile) || !File.Exists(dialog.ModFile))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                    {
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        Message = i18n["Instance.Mods.Import.Error.InvalidPath"]
                    });
                    return;
                }

                try 
                {
                    var modFileName = Path.GetFileName(dialog.ModFile);
                    var targetPath = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods", modFileName);

                    // 确保目标目录存在
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (targetDir != null && !Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    File.Copy(dialog.ModFile, targetPath, true);

                    ModsManager.AddMod(new ModInfo
                    {
                        File = targetPath,
                        InjectDelay = dialog.ModDelay,
                        IsPreLoad = dialog.IsPreLoad
                    });
                    UpdateUI();
                }
                catch (Exception ex)
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                    {
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        Message = $"{i18n["Instance.Mods.Import.Error.CopyFailed"]}\n{ex.Message}"
                    });
                }
            }
        });
    }
}