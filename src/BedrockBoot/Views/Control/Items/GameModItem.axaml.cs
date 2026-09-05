using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Core.Models.Pack.Game.Mods;
using BedrockBoot.Interface;
using BedrockBoot.Interface.ModLoader;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Loaders.LoaderInstance;
using BedrockBoot.Models.Pack.Game.Loaders.ModsManagers;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class GameModItem : UserControl
{
    private readonly IModsLoader _modsLoader;

    public GameModItem()
    {
        InitializeComponent();
    }

    public GameModItem(ModItemInfo info, IModsLoader modsLoader) : this()
    {
        ModInfo = info;
        _modsLoader = modsLoader;
        UpdateUI();
    }

    private static I18nManager i18n => I18nManager.Instance;

    public ModItemInfo ModInfo { get; private set; }
    public VersionConfig VersionConfig => _modsLoader.GameInstance;
    public Action? UpdateCallBack { get; set; }

    public void UpdateUI()
    {
        if (ModInfo == null) return;

        if (_modsLoader.GetType() != typeof(PreLoaderNet)) SettingBtn.IsVisible = false;
        FileName.Text = ModInfo.ModName;

        var fileSize = File.Exists(ModInfo.ModPath) ? new FileInfo(ModInfo.ModPath).Length : 0;
        var formattedSize = SizeHelper.FormatBytes(fileSize);

        VersionBox.IsVisible = !string.IsNullOrEmpty(ModInfo.Version);
        if (!string.IsNullOrEmpty(ModInfo.Version))
            VersionBox.Text = ModInfo.Version;

        var descs = new List<string>();

        if (ModInfo.ModInjectType == ModType.Inject)
            descs.Add($"{ModInfo.InjectDelay} ms");
        else
        {
            if (ModInfo.ModLoaderType == typeof(LeviLamina))
            {
                if (!string.IsNullOrEmpty(ModInfo.ModDescription))
                    descs.Add(ModInfo.ModDescription);
            }
        }

        descs.Add(formattedSize);
        Card.Description = string.Join(", ", descs);

        PreLoadBox.IsVisible =
            (ModInfo.ModInjectType == ModType.Native && ModInfo.ModLoaderType == typeof(PreLoaderNet));
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Mod.Delete.Title"],
            Content = $"{i18n["Instance.Mod.Delete.Content"]} {ModInfo.ModName}?\n{i18n["Common.Action.Irreversible"]}",
            CloseButtonText = i18n["MainWindow.Common.Confirm"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = () =>
            {
                try
                {
                    _modsLoader.ModsManager.Remove(ModInfo);

                    UpdateCallBack?.Invoke();
                }
                catch (Exception ex)
                {
                    DialogHost.Show(new DialogInfo
                    {
                        Title = i18n["MainWindow.Dialog.Error.Title"],
                        Content = $"{i18n["Instance.Mod.Delete.Error"]} {ModInfo.ModName}:\n{ex.Message}",
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
            IsPreLoad = ModInfo.ModInjectType == ModType.Native,
            ModDelay = ModInfo.InjectDelay,
            ModFile = ModInfo.ModPath
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

                var modsDir = Path.Combine(VersionConfig.VersionPath, "config", "BedrockBoot2", "mods");
                if (!Directory.Exists(modsDir)) Directory.CreateDirectory(modsDir);

                var targetPath = Path.Combine(modsDir, Path.GetFileName(dialog.ModFile));

                try
                {
                    if (ModInfo.ModPath != targetPath)
                    {
                        if (File.Exists(ModInfo.ModPath)) File.Delete(ModInfo.ModPath);
                        File.Copy(dialog.ModFile, targetPath, true);
                    }

                    var index = _modsLoader.ModsManager.GetAllMods().FindIndex(x => x.ModPath == ModInfo.ModPath);

                    if (index != -1)
                    {
                        ((PreLoaderModsManager)_modsLoader.ModsManager).ModsManager.ModsConfig.Data[index] = new ModInfo
                        {
                            File = targetPath,
                            InjectDelay = dialog.ModDelay,
                            IsPreLoad = dialog.IsPreLoad
                        };
                        ((PreLoaderModsManager)_modsLoader.ModsManager).ModsManager.ModsConfig.Save();

                        var newModInfo =
                            ((PreLoaderModsManager)_modsLoader.ModsManager).ModsManager.ModsConfig.Data[index];
                        ModInfo = new()
                        {
                            InjectDelay = newModInfo.InjectDelay,
                            ModInjectType = newModInfo.IsPreLoad ? ModType.Native : ModType.Inject,
                            ModPath = newModInfo.File,
                            ModName = Path.GetFileName(newModInfo.File),
                            ModLoaderType = typeof(PreLoaderNet)
                        };
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