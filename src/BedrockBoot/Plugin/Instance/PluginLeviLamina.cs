using System;
using System.IO;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Interface.Instance;
using BedrockBoot.LeviLamina.Models.Installer;
using BedrockBoot.Views.DialogContent.Plugin.LeviLamina;
using BedrockBoot.Views.TaskItem.Plugin.LeviLamina;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Plugin.Instance;

public class PluginLeviLamina : IInstancePlugin
{
    private static I18nManager i18n => I18nManager.Instance;

    public string Name { get; set; } = "LeviLamina";
    public string Description { get; set; } = "基岩版客户端模组加载器";
    public string Icon { get; set; } = "avares://BedrockBoot/Assets/Icon/Other/LeviLauncher.png";
    public VersionConfig VersionConfig { get; set; } = null!;

    public void Init(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
    }

    /// <summary>
    /// 检测当前实例是否已安装 LeviLamina
    /// </summary>
    public bool IsInstalled()
    {
        if (VersionConfig == null || string.IsNullOrEmpty(VersionConfig.VersionPath))
            return false;

        // 检测是否存在核心 DLL 文件
        return File.Exists(Path.Combine(VersionConfig.VersionPath, "mods", "LeviLamina.dll"));
    }

    /// <summary>
    /// 触发安装/版本选择流程
    /// </summary>
    public async Task Install()
    {
        // 显示加载版本中的占位对话框
        DialogHost.Show(new DialogInfo
        {
            Content = i18n["Plugin.LeviLamina.LoadingVersions"],
            Title = Name
        });

        var llmInstaller = new LeviLaminaInstaller(VersionConfig);
        
        try
        {
            // 获取远程版本列表
            var versions = await llmInstaller.GetVersions();
            await DialogHost.Close();

            // 弹出版本选择对话框
            var chooseDialog = new DialogLeviLaminaChooseVersionContent(versions);
            DialogHost.Show(new DialogInfo
            {
                Content = chooseDialog,
                Title = i18n["Plugin.LeviLamina.ChooseVersion"],
                CloseButtonText = i18n["MainWindow.Common.Confirm"],
                PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
                CloseAction = () =>
                {
                    var selectedVersion = chooseDialog.Version;
                    if (selectedVersion != null)
                    {
                        // 触发下载与安装后台任务
                        TaskInstallLeviLaminaItem.Install(selectedVersion, VersionConfig);
                    }
                }
            });
        }
        catch (NullReferenceException)
        {
            await DialogHost.Close();
            DialogHost.Show(new DialogInfo
            {
                Content = i18n["Plugin.LeviLamina.NotSupport"],
                Title = Name,
                CloseButtonText = i18n["MainWindow.Common.Confirm"]
            });
        }
        catch (Exception ex)
        {
            await DialogHost.Close();
            DialogHost.Show(new DialogInfo
            {
                Content = $"{i18n["MainWindow.Dialog.Error.Title"]}: {ex.Message}",
                Title = Name,
                CloseButtonText = i18n["MainWindow.Common.Confirm"]
            });
        }
    }
}