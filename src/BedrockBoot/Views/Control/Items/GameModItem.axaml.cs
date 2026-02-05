using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Mods;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class GameModItem : UserControl
{
    public GameModItem()
    {
        InitializeComponent();
    }

    public GameModItem(ModInfo info,VersionConfig versionConfig) : this()
    {
        ModInfo = info;
        VersionConfig = versionConfig;

        UpdateUI();
    }

    public ModInfo ModInfo { get; set; }
    public ModsManager ModsManager { get; set; }
    public VersionConfig VersionConfig { get; set; }
    public Action? UpdateCallBack { get; set; }

    public void UpdateUI()
    {
        FileName.Text = Path.GetFileName(ModInfo.File);

        if (!ModInfo.IsPreLoad)
            Card.Description = $"{SizeHelper.FormatBytes(new FileInfo(ModInfo.File).Length)}，{ModInfo.InjectDelay} ms";
        else
            Card.Description = $"{SizeHelper.FormatBytes(new FileInfo(ModInfo.File).Length)}";

        PreLoadBox.IsVisible = ModInfo.IsPreLoad;
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = "删除模组",
            Content = $"您确定要删除模组 {Path.GetFileName(ModInfo.File)} 吗\n" +
                      $"这将永远无法恢复。",
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                try
                {
                    File.Delete(ModInfo.File);
                    ModsManager.RefreshMods(true);
                }
                catch (Exception e)
                {
                    DialogHost.Show(new DialogInfo
                    {
                        Title = "出现错误",
                        Content = $"删除模组 {Path.GetFileName(ModInfo.File)} 时\n" +
                                  $"出现错误：{e.Message}",
                        CloseButtonText = "确定"
                    });
                }
            }
        });
    }

    private void SettingBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogImportModContent();
        dialog.IsPreLoad = ModInfo.IsPreLoad;
        dialog.ModDelay = ModInfo.InjectDelay;
        dialog.ModFile = ModInfo.File;
        
        DialogHost.Show(new DialogInfo
        {
            Title = "设置 Mod 文件",
            Content = dialog,
            CloseButtonText = "保存",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                if (string.IsNullOrEmpty(dialog.ModFile) ||
                    !File.Exists(dialog.ModFile))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                    {
                        Message = "无效路径，无法添加模组",
                        Title = "无效路径"
                    });
                    return;
                }

                var path = Path.Combine(VersionConfig.VersionPath, "config", "BedrockBoot2", "mods",
                    Path.GetFileName(dialog.ModFile));
                if (dialog.ModFile != path)
                {
                    File.Delete(ModInfo.File);
                    File.Copy(dialog.ModFile, path);
                }

                var index = ModsManager.ModsConfig.Data.FindIndex(x => x.File == ModInfo.File &&
                                                                       x.InjectDelay == ModInfo.InjectDelay &&
                                                                       x.IsPreLoad == ModInfo.IsPreLoad);

                ModsManager.ModsConfig.Data[index] = new ModInfo
                {
                    File = path,
                    InjectDelay = dialog.ModDelay,
                    IsPreLoad = dialog.IsPreLoad
                };
                ModsManager.ModsConfig.Save();
                UpdateCallBack?.Invoke();
            }
        });
    }
}