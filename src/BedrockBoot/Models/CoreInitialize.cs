using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Manifest;
using BedrockBoot.Entity;
using BedrockBoot.Models.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Helper.Uwp;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models;

public class CoreInitialize
{
    private static I18nManager I18n => I18nManager.Instance;
    public static async Task Init()
    {
        CheckUserAgreement();
        if (!Core.Global.GlobalModel.Config.Data.IsAgreeTerms) return;
        
        // 加载功能配置文件
        try
        {
            BedrockBoot.Models.Global.GlobalModel.FunctionOption = await new JsonResourceEntity()
                .LoadJsonResourceAsync<FunctionOptionEntry>(
                    "avares://BedrockBoot/Manifest/Function/FunctionOption.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to load FunctionOption: {ex.Message}");
        }
        
        // 核心引擎异步初始化
        _ = InitBedrockCoreAsync();

#if WINDOWS
        // 注册文件关联
        HandleFileAssociations();
#endif
        _ = GetDevelopMode();
        CheckUwpDependence();
    }

    private static async Task InitBedrockCoreAsync()
    {
        try
        {
            await CoreInit.Init();

            CoreInit.UpdateUseHardwareDecode(Core.Global.GlobalModel.Config.Data.IsUseHardwareDecode);
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