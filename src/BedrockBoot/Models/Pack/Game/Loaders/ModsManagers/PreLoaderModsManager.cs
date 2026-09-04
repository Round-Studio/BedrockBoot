using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Core.Models.Pack.Game.Mods;
using BedrockBoot.Interface;
using BedrockBoot.Interface.ModLoader;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Loaders.LoaderInstance;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Pack.Game.Loaders.ModsManagers;

public class PreLoaderModsManager : IModsManager
{
    public VersionConfig _instance;
    public ModsManager ModsManager;
    private static I18nManager i18n => I18nManager.Instance;

    public void Init(VersionConfig instance)
    {
        _instance = instance;
        ModsManager = new ModsManager(_instance);
    }

    public Action? OnRefresh { get; set; }

    public List<ModItemInfo> GetAllMods()
    {
        return ModsManager.RefreshMods().Select(x =>
        {
            return new ModItemInfo()
            {
                ModName = Path.GetFileName(x.File),
                ModDescription = string.Empty,
                ModLoaderType = typeof(PreLoaderNet),
                ModInjectType = x.IsPreLoad ? ModType.Native : ModType.Inject,
                InjectDelay = x.InjectDelay,
                ModPath = x.File
            };
        }).ToList();
    }

    public async Task AddMod()
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
                    var targetPath = Path.Combine(_instance.VersionPath, "config", "BedrockBoot2", "mods",
                        modFileName);

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
                    OnRefresh?.Invoke();
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

    public void Remove(ModItemInfo info)
    {
        ModsManager.ModsConfig.Data.Remove(new ModInfo()
        {
            File = info.ModPath,
            InjectDelay = info.InjectDelay,
            IsPreLoad = info.ModInjectType == ModType.Native
        });
        ModsManager.ModsConfig.Save();

        if (File.Exists(info.ModPath))
            File.Delete(info.ModPath);
        OnRefresh?.Invoke();
    }
}