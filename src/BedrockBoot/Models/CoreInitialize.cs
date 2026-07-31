using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Manifest;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Entity;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Options;
using BedrockBoot.Proton;
using BedrockBoot.Views.Control.Widgets.DesktopWidgets;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Linux;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Plugin.BedrockBoot.Register;
#if WINDOWS
using BedrockBoot.Models.Helper.Gdk;
using BedrockBoot.Models.Helper.Uwp;
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
            Console.WriteLine(@"开始同步游戏配置文件");
            var config = GameInfoHelper.GetVersionConfig(path);
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

#if LINUX
        ProtonCore.InitializeEnvironment();
        
        var lst = ProtonCore.GetInstalledVersions();
        if (lst == null || lst.Count <= 0 || !ProtonNeoCore.IsInstalledKits())
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
            await CoreInit.Init();

            CoreInit.UpdateUseHardwareDecode(Core.Global.GlobalModel.Config.Data.IsUseHardwareDecode);
#if LINUX
            CoreInit.UpdateUseNeoLaunch(Core.Global.GlobalModel.Config.Data.IsUseNeoLaunch);
            RegisterService.RegisterLaunchingEvent(s =>
            {
                CoreInit.SetMsAccount(
                    MsAccountManager.Accounts.Accounts.Find(x => x.BUID == MsAccountManager.Accounts.SelectUserBUID));
            });
            CoreInit.OnRefreshAccount = async void (account) =>
            {
                if (account == null)
                {
                    var accounts = MsAccountManager.Accounts;
                    account =
                        MsAccountManager.Accounts.Accounts.Find(x =>
                            x.BUID == MsAccountManager.Accounts.SelectUserBUID);
                    CoreInit.SetMsAccount(account);
                };
                Console.WriteLine("正在刷新账户凭证...");
                var client = new MsaDeviceCodeClient();
                var tokenData = await client.RefreshTokenAsync(account.AuthResult.RefreshToken);
                Console.WriteLine("刷新完毕。");

                if (tokenData != null)
                {
                    var index = MsAccountManager.Accounts.Accounts.FindIndex(x => x.BUID == account.BUID);
                    MsAccountManager.AccountConfigEntity.Data.Accounts[index].AuthResult = new()
                    {
                        Code = tokenData?.Code,
                        AccessToken = tokenData?.AccessToken,
                        ClientId = tokenData?.ClientId,
                        CodeVerifier = tokenData?.CodeVerifier,
                        ExpiresIn = (int)tokenData.ExpiresIn,
                        RedirectUri = tokenData?.RedirectUri,
                        RefreshToken = tokenData.RefreshToken,
                        SavedAt = DateTime.Now
                    };
                
                    MsAccountManager.AccountConfigEntity.Save();
                    CoreInit.SetMsAccount(MsAccountManager.AccountConfigEntity.Data.Accounts[index]);
                    Console.WriteLine("新用户数据已保存");
                }
            };
            CoreInit.SetMsAccount(
                MsAccountManager.Accounts.Accounts.Find(x => x.BUID == MsAccountManager.Accounts.SelectUserBUID));
#endif
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