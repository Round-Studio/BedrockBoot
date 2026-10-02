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
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity.Manifest;
using BedrockBoot.Standard.Enum.Type;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Helper.GravityCone;
using BedrockBoot.Models.Pack.Game.Loaders;
using BedrockBoot.Models.Pack.Game.Options;
using BedrockBoot.Standard.Core;
using BedrockBoot.Standard.Entity;
using BedrockBoot.Standard.Interface.ModLoader;
using BedrockBoot.Views.Control.Widgets.DesktopWidgets;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Plugin.BedrockBoot.Register;
#if WINDOWS
using BedrockBoot.Windows;
using BedrockBoot.Windows.Models.Helper;
using BedrockBoot.Windows.Models.Helper.Gdk;
using BedrockBoot.Windows.Models.Helper.Uwp;
#endif
#if LINUX
using BedrockBoot.Linux;
#endif

namespace BedrockBoot.Models;

public class CoreInitialize
{
    private static I18nManager I18n => I18nManager.Instance;

    public static async Task Init()
    {
        DesktopWorkspace.WidgetRegister(new()
        {
            Name = "时钟",
            Description = "一个非常普通的时间显示组件",
            Type = WidgetType.Timer,
            WidgetTypeof = typeof(WidgetTimer),
            DefaultSize = WidgetSize.Small
        });
        DesktopWorkspace.WidgetRegister(new()
        {
            Name = "最近游玩",
            Description = "显示最近游玩的一个游戏实例",
            Type = WidgetType.LeastPlay,
            WidgetTypeof = typeof(WidgetLaunchGame),
            DefaultSize = WidgetSize.Large
        });

        CheckUserAgreement();
        if (!Core.Global.GlobalModel.Config.Data.IsAgreeTerms) return;

        // 加载功能配置文件
        try
        {
            GlobalModel.FunctionOption = await new JsonResourceEntity()
                .LoadJsonResourceAsync<FunctionOptionEntry>(
                    "avares://BedrockBoot/Manifest/Function/FunctionOption.json");
            GlobalModel.CustomManifest = await new JsonResourceEntity()
                .LoadJsonResourceAsync<CustomManifest>(
                    "avares://BedrockBoot/Manifest/DefaultCustomManifest.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to load FunctionOption: {ex}");
        }

        // 核心引擎异步初始化
        _ = InitBedrockCoreAsync();

#if WINDOWS
        // 注册文件关联
        HandleFileAssociations();
#endif
        Task.Run(() =>
        {
            _ = GetDevelopMode();
            CheckUwpDependence();
        });

        RegisterService.API.LaunchingEvent.Add(path =>
        {
            File.WriteAllText(Path.Combine(path, "config", "BedrockBoot2", ".bb.version"), GlobalModel.BodyVersion);

            File.WriteAllText(Path.Combine(path, "config", ".launcher.info"),
                Process.GetCurrentProcess().MainModule!.FileName);
            var config = GameInfoHelper.GetVersionConfig(path);
            GamePortHelper.AddInstance(config);
            Console.WriteLine(@"开始同步游戏配置文件");
            Console.WriteLine($@"当前实例配置：{config.Config.IsSyncPublicOptions}");
            if (config.Config.IsSyncPublicOptions)
            {
                if (Core.Global.GlobalModel.Config.Data.PublicOptionsConfig == null) return;
                if (Core.Global.GlobalModel.Config.Data.PublicOptionsConfig.PubOptionsInstancePath != null &&
                    Core.Global.GlobalModel.Config.Data.PublicOptionsConfig.PubUser != null)
                {
                    var sourceManager =
                        new GameOptionsManager(GameInfoHelper.GetVersionConfig(Core.Global.GlobalModel.Config.Data
                            .PublicOptionsConfig.PubOptionsInstancePath));

                    var aimManager = new GameOptionsManager(GameInfoHelper.GetVersionConfig(path));
                    aimManager.GetUsers().ForEach(user =>
                    {
                        aimManager.SaveGameOptions(
                            sourceManager.GetGameOptions(
                                Core.Global.GlobalModel.Config.Data.PublicOptionsConfig.PubUser), user);
                    });
                }
            }
        });

        RegisterService.API.LaunchingEvent.Add(path =>
        {
            var conf = GameInfoHelper.GetVersionConfig(path);
            if (!conf.Config.IsModes) return;
            LoadersManager.ModsLoaders.ForEach(loader =>
            {
                if (typeof(IModsLoader).IsAssignableFrom(loader))
                {
                    var instance = (IModsLoader)Activator.CreateInstance(loader);
                    instance.InitLoader(conf);
                    try
                    {
                        instance.PreLaunch();
                        Console.WriteLine($@"模组加载器 {instance.LoaderName} 准备完毕");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"准备模组加载器 {instance.LoaderName} 时出现错误：{ex}");
                    }
                }
            });
        });

#if LINUX
        ProtonCore.InitializeEnvironment();
        
        var lst = ProtonCore.GetInstalledVersions();
        if (!ProtonNeoCore.IsInstalledKits())
        {
            DialogHost.Show(new DialogInfo()
            {
                Content = "当前您正在 Linux 环境下运行本启动器\n" +
                          "我们需要 ProtonGDK 组件才能正常启动 Minecraft for Windows (GDK)\n" +
                          "\n" +
                          "现在我们需要您同意 ProtonGDK 组件的下载",
                Title = "必要运行时下载",
                CloseButtonText = "立即下载",
                PrimaryButtonText = "退出启动器",
                AccountButton = DialogButtons.CloseButton,
                PrimaryAction = () =>
                {
                    Console.WriteLine("用户不同意下载 ProtonGDK，正在退出启动器...");
                    Environment.Exit(0);
                },
                CloseAction = () =>
                {
                    var dialog = new DialogDownloadProtonGDKContent();
                    DialogHost.Show(new DialogInfo()
                    {
                        Content = dialog,
                        Title = "下载游戏运行组件"
                    });
                    dialog.Download();
                }
            });
        }
#endif
    }

    private static async Task InitBedrockCoreAsync()
    {
        try
        {
#if LINUX
            WindowsCoreInit.UpdateUseNeoLaunch(Core.Global.GlobalModel.Config.Data.IsUseNeoLaunch);
#endif
            PlatformCore.GetMsAccountConfig = () =>
            {
                if (Core.Global.GlobalModel.Config.Data.IsChooseAccountBeforeLaunch)
                {
                    var user = GlobalModel.MainWindow.ChooseAccount().Result;
                    return user;
                }
                else
                {
                    if (MsAccountManager.Accounts!.Accounts.Count >= 1)
                    {
                        var account = MsAccountManager.Accounts.Accounts.Find(x =>
                            x.BUID == MsAccountManager.Accounts.SelectUserBUID);

                        return account;
                    }
                    else
                    {
                        DialogHost.Show(new()
                        {
                            Title = "无账户",
                            Content = "当前未登录微软账户，请前往账户管理页面登录账户以启动游戏",
                            CloseButtonText = "确定"
                        });
                    }
                }

                return null;
            };
            PlatformCore.OnRefreshAccount = async account =>
            {
                if (account == null)
                {
                    account =
                        MsAccountManager.Accounts.Accounts.Find(x =>
                            x.BUID == MsAccountManager.Accounts.SelectUserBUID);
                }

                Console.WriteLine(@"正在刷新账户凭证...");
                var refreshed = await MsAccountManager.RefreshAccountAsync(account);
                Console.WriteLine(@"刷新完毕。");

                return refreshed;
            };

            var coreInitUnit =
#if WINDOWS
                new WindowsCoreInit();     
#elif LINUX
                new LinuxCoreInit();
#endif
            await PlatformCore.InstallAsync(coreInitUnit);
            PlatformCore.CoreInit?.UpdateUseHardwareDecode(Core.Global.GlobalModel.Config.Data.IsUseHardwareDecode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"BedrockCore Init Error: {ex}");

            if (ex.Message.Contains("Not Support Windows Version"))
                await Dispatcher.UIThread.InvokeAsync(() => DialogHost.Show(new DialogInfo
                {
                    Title = I18n["MainWindow.Dialog.UnsupportedSys.Title"],
                    Content = I18n["MainWindow.Dialog.UnsupportedSys.Content"],
                    CloseButtonText = I18n["MainWindow.Dialog.UnsupportedSys.Close"],
                    CloseAction = () => Environment.Exit(1)
                }));
        }
    }

    private static void CheckUserAgreement()
    {
        if (Core.Global.GlobalModel.Config.Data.IsAgreeTerms) return;

        DialogHost.Show(new DialogInfo
        {
            Content = new DialogAgreementContent(),
            Title = I18n["MainWindow.Dialog.Agreement.Title"],
            CloseButtonText = I18n["MainWindow.Dialog.Agreement.Agree"],
            CloseAction = () =>
            {
                Core.Global.GlobalModel.Config.Data.IsAgreeTerms = true;
                Core.Global.GlobalModel.Config.Save();

                _ = Init();
            },
            PrimaryButtonText = I18n["MainWindow.Dialog.Agreement.Decline"],
            PrimaryAction = () => Environment.Exit(0),
            AccountButton = DialogButtons.CloseButton
        });
    }

    private static void HandleFileAssociations()
    {
#if RELEASE
        if (GlobalModel.FunctionOption?.IsEnableMcPackOpenWithBody == true)
            OpenAgreement.RegisterAssociation();
#else
        OpenAgreement.RegisterAssociation();
#endif
    }

    private static async Task GetDevelopMode()
    {
#if WINDOWS
        var devMod = DeveloperModeHelper.IsDeveloperModeViaPowerShell();
        if (!devMod)
            DeveloperModeHelper.ShowNotice();
#endif
    }

    public static async Task GetSdkInstalledMode()
    {
#if WINDOWS
        if (!AppSdkChecker.GetInstalled())
        {
            var dialogInfo = new DialogInfo
            {
                Title = "未安装 SDK 1.8",
                Content = "当前系统未检测到完整的 Windows App SDK 1.8 (8000.x) 组件。\n" +
                          "缺失组件可能包括: Main, Singleton 或 DDLM。\n" +
                          "这会导致游戏无法启动。",
                CloseButtonText = "立即安装",
                PrimaryButtonText = "放任不管",
                AccountButton = DialogButtons.CloseButton,

                CloseAction = () =>
                {
                    DialogHost.Show(new()
                    {
                        Title = "下载 SDK",
                        Content = new DialogDownloadAppSdkContent()
                    });
                },
            };
            DialogHost.Show(dialogInfo);
        }
        else
        {
            DialogHost.Show(new()
            {
                Title = "您已安装 SDK 1.8",
                Content = "您已安装 SDK 1.8，可无需再次安装",
                CloseButtonText = "确定"
            });
        }
#endif
    }

    private static void CheckUwpDependence()
    {
#if WINDOWS
        Task.Run(() =>
        {
            Thread.Sleep(1000);
            var depList = UwpDependencyChecker.GetMissingDependencies();
            if (depList.Count > 0)
            {
                Console.WriteLine($@"当前系统未安装对应的 UWP 依赖，共 {depList.Count} 个依赖未安装。");
                Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Title = "安装 UWP 依赖",
                        Content = new DialogDownloadUwpDependenceContent(depList)
                    });
                });
            }
        });
#endif
    }
}