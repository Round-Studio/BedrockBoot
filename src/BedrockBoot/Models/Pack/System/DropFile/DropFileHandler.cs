using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Base.Enum.Type;
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
            case SupportedFileType.Rplck:
                HandelPluginPacks();
                break;
            case SupportedFileType.Rskin:
                HandelSkinPacks();
                break;
        }
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
                    Title = "选择实例以导入资源包",
                    Content = ins,
                    CloseButtonText = "确定",
                    PrimaryButtonText = "取消",
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
            Title = "选择实例以导入存档包",
            Content = ins,
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
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
                        Title = "存档已导入",
                        Message = "所有存档已导入目标游戏实例中"
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
            Title = "插件包导入完毕",
            Content = "Round Studio 通用插件包导入完毕。\n" +
                      $"本次导入 {_files.Count} 个插件。\n" +
                      $"可前往 启动器设置>插件>管理 中管理已安装的插件",
            CloseButtonText = "确定",
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
            Title = "主题包导入完毕",
            Content = "Round Studio 通用主题包导入完毕。\n" +
                      $"本次导入 {_files.Count} 个主题。\n" +
                      $"可前往 启动器设置>个性化>主题包 中管理已安装的主题",
            CloseButtonText = "确定",
            AccountButton = DialogButtons.CloseButton
        });
    }
}