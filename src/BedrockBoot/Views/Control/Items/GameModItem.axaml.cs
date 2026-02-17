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
    private static I18nManager i18n => I18nManager.Instance;

    public GameModItem()
    {
        InitializeComponent();
    }

    public GameModItem(ModInfo info, VersionConfig versionConfig) : this()
    {
        ModInfo = info;
        VersionConfig = versionConfig;

        UpdateUI();
    }

    public ModInfo ModInfo { get; set; }
    public ModsManager ModsManager { get; set; } = null!;
    public VersionConfig VersionConfig { get; set; } = null!;
    public Action? UpdateCallBack { get; set; }

    public void UpdateUI()
    {
        if (ModInfo == null) return;

        string fileName = Path.GetFileName(ModInfo.File);
        FileName.Text = fileName;

        long fileSize = File.Exists(ModInfo.File) ? new FileInfo(ModInfo.File).Length : 0;
        string formattedSize = SizeHelper.FormatBytes(fileSize);

        if (!ModInfo.IsPreLoad)
            Card.Description = $"{formattedSize}, {ModInfo.InjectDelay} ms";
        else
            Card.Description = formattedSize;

        PreLoadBox.IsVisible = ModInfo.IsPreLoad;
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        string fileName = Path.GetFileName(ModInfo.File);
        
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Mod.Delete.Title"],
            Content = $"{i18n["Instance.Mod.Delete.Content"]} {fileName}?\n{i18n["Common.Action.Irreversible"]}",
            CloseButtonText = i18n["MainWindow.Common.Confirm"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = () =>
            {
                try
                {
                    if (File.Exists(ModInfo.File))
                        File.Delete(ModInfo.File);
                    
                    ModsManager.RefreshMods(true);
                    UpdateCallBack?.Invoke();
                }
                catch (Exception ex)
                {
                    DialogHost.Show(new DialogInfo
                    {
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        Content = $"{i18n["Instance.Mod.Delete.Error"]} {fileName}:\n{ex.Message}",
                        CloseButtonText = i18n["MainWindow.Common.Confirm"]
                    });
                }
            }
        });
    }

    private void SettingBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogImportModContent
        {
            IsPreLoad = ModInfo.IsPreLoad,
            ModDelay = ModInfo.InjectDelay,
            ModFile = ModInfo.File
        };

        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Mod.Setting.Title"],
            Content = dialog,
            CloseButtonText = i18n["MainWindow.Common.Save"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = () =>
            {
                if (string.IsNullOrEmpty(dialog.ModFile) || !File.Exists(dialog.ModFile))
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                    {
                        Message = i18n["Instance.Mod.Import.InvalidPath"],
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        NoticeType = NoticeType.Error
                    });
                    return;
                }

                string modsDir = Path.Combine(VersionConfig.VersionPath, "config", "BedrockBoot2", "mods");
                if (!Directory.Exists(modsDir)) Directory.CreateDirectory(modsDir);

                string targetPath = Path.Combine(modsDir, Path.GetFileName(dialog.ModFile));

                try
                {
                    if (ModInfo.File != targetPath)
                    {
                        if (File.Exists(ModInfo.File)) File.Delete(ModInfo.File);
                        File.Copy(dialog.ModFile, targetPath, true);
                    }

                    var index = ModsManager.ModsConfig.Data.FindIndex(x => x.File == ModInfo.File);

                    if (index != -1)
                    {
                        ModsManager.ModsConfig.Data[index] = new ModInfo
                        {
                            File = targetPath,
                            InjectDelay = dialog.ModDelay,
                            IsPreLoad = dialog.IsPreLoad
                        };
                        ModsManager.ModsConfig.Save();
                        
                        ModInfo = ModsManager.ModsConfig.Data[index];
                        UpdateUI();
                        UpdateCallBack?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                    {
                        Message = ex.Message,
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        NoticeType = NoticeType.Error
                    });
                }
            }
        });
    }
}