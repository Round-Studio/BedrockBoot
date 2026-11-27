/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Standard.Enum.Type;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Models.Pack.Plugin;
using BedrockBoot.Models.Pack.Theme;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Pack.System.DropFile;

public class DropFileHandler
{
    private readonly List<string> _files;
    private static I18nManager i18n => I18nManager.Instance;
    
    public DropFileHandler(List<string> files)
    {
        _files = files;
    }

    public void Handle()
    {
        switch (DropFileCheck.GetFileType(_files[0])) // 反正都是同一种文件...所以拿第一个就好了
        {
            case SupportedFileType.Mcaddon:
            case SupportedFileType.Mcpack:
                HandelMcPacks();
                break;
            case SupportedFileType.Mcworld:
                HandelMcWorld();
                break;
            case SupportedFileType.Mctemplate:
                HandelMcTemplate();
                break;
            case SupportedFileType.Rplck:
                HandelPluginPacks();
                break;
            case SupportedFileType.Rskin:
                HandelSkinPacks();
                break;
        }
    }

    public void HandelMcTemplate()
    {
        Console.WriteLine(@"处理拖入的基岩版世界模版包文件");

        var ins = new DialogChooseGameContent(true);
        DialogHost.Show(new()
        {
            Title = I18nManager.Instance["DropFile.ChooseTemplate"],
            Content = ins,
            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = async () =>
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = i18n["Instance.Pack.Import.Progress.Title"],
                    Content = i18n["Instance.Pack.Import.Progress.Content"]
                });

                var manager = new ResourcePackManager(ins.VersionConfig);

                // 导入操作在后台执行
                await Task.Run(() => { _files.ForEach(f => manager?.AddRangePacks(new() { f }, ins.SelectUser)); });

                Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Close();
                    GlobalModel.MainWindow.Notice.AddNotice(new()
                    {
                        Title = I18nManager.Instance["DropFile.Template.Imported"],
                        Message = I18nManager.Instance["DropFile.Template.ImportedDesc"]
                    });
                });
            }
        });
    }

    public void HandelMcPacks()
    {
        Console.WriteLine(@"处理拖入的基岩版支持包文件");
        
        var body = new DialogImportResourcePackContent();

        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Pack.Import.Dialog.Title"],
            Content = body,
            CloseButtonText = i18n["Instance.Pack.Import.Dialog.Action"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = async () =>
            {
                var ins = new DialogChooseGameContent();
                DialogHost.Show(new()
                {
                    Title = I18nManager.Instance["DropFile.ChooseResource"],
                    Content = ins,
                    CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
                    PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
                    AccountButton = DialogButtons.CloseButton,
                    CloseAction = async () =>
                    {
                        DialogHost.Show(new DialogInfo
                        {
                            Title = i18n["Instance.Pack.Import.Progress.Title"],
                            Content = i18n["Instance.Pack.Import.Progress.Content"]
                        });

                        var resourcePackManager = new ResourcePackManager(ins.VersionConfig);

                        // 导入操作在后台执行
                        await Task.Run(() => { resourcePackManager?.AddRangePacks(_files); });

                        Dispatcher.UIThread.Invoke(() =>
                        {
                            DialogHost.Close();
                        });
                    }
                });
            }
        });
        body.Import(_files);
    }
    public void HandelMcWorld()
    {
        Console.WriteLine(@"处理拖入的基岩版存档包文件");
        
        var ins = new DialogChooseGameContent();
        DialogHost.Show(new()
        {
            Title = I18nManager.Instance["DropFile.ChooseSave"],
            Content = ins,
            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = async () =>
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = i18n["Instance.Pack.Import.Progress.Title"],
                    Content = i18n["Instance.Pack.Import.Progress.Content"]
                });

                var manager = new ArchiveCheck(ins.VersionConfig);

                // 导入操作在后台执行
                await Task.Run(() => { _files.ForEach(f => manager?.ImportWorldPack(f)); });

                Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Close();
                    GlobalModel.MainWindow.Notice.AddNotice(new ()
                    {
                        Title = I18nManager.Instance["DropFile.Save.Imported"],
                        Message = I18nManager.Instance["DropFile.Save.ImportedDesc"]
                    });
                });
            }
        });
    }
    public void HandelPluginPacks()
    {
        Console.WriteLine(@"处理拖入的插件包文件");
        
        _files.ForEach(f =>
        {
            _ = PluginLoader.Install(f);
        });
        
        DialogHost.Show(new()
        {
            Title = I18nManager.Instance["DropFile.Plugin.Imported"],
            Content = I18nManager.Instance["DropFile.Plugin.ImportedDesc"] +
                      string.Format(I18nManager.Instance["DropFile.Plugin.Count"], _files.Count) +
                      I18nManager.Instance["DropFile.Plugin.ManageHint"],
            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
            AccountButton = DialogButtons.CloseButton
        });
    }
    public void HandelSkinPacks()
    {
        Console.WriteLine(@"处理拖入的主题包文件");

        var manager = new ThemePackManager();
        
        _files.ForEach(f =>
        {
            manager.AddPack(f);
        });
        
        DialogHost.Show(new()
        {
            Title = I18nManager.Instance["DropFile.Theme.Imported"],
            Content = I18nManager.Instance["DropFile.Theme.ImportedDesc"] +
                      string.Format(I18nManager.Instance["DropFile.Theme.Count"], _files.Count) +
                      I18nManager.Instance["DropFile.Theme.ManageHint"],
            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
            AccountButton = DialogButtons.CloseButton
        });
    }
}