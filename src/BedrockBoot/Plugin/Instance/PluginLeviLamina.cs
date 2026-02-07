using System;
using System.IO;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Interface.Instance;
using BedrockBoot.LeviLamina.Models.Installer;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Plugin.Instance;

public class PluginLeviLamina : IInstancePlugin
{
    public string Name { get; set; } = "LeviLamina";
    public string Description { get; set; } = "基岩版客户端模组加载器";
    public string Icon { get; set; } = "avares://BedrockBoot/Assets/Icon/Other/LeviLauncher.png";
    public VersionConfig VersionConfig { get; set; }

    public void Init(VersionConfig versionConfig)
    {
        VersionConfig = versionConfig;
    }

    public bool IsInstalled()
    {
        if (File.Exists(Path.Combine(VersionConfig.VersionPath, "mods", "LeviLamina", "LeviLamina.dll")))
            return true;

        return false;
    }

    public async Task Install()
    {
        DialogHost.Show(new DialogInfo()
        {
            Content = "加载版本中...",
            Title = "LeviLamina"
        });

        var llmInstaller = new LeviLaminaInstaller(VersionConfig);
        
        try
        {
            var versions = await llmInstaller.GetVersions();
            await DialogHost.Close();
        }
        catch (NullReferenceException nullEx)
        {
            await DialogHost.Close();
            DialogHost.Show(new DialogInfo()
            {
                Content = "该实例不支持安装 LeviLamina",
                Title = "LeviLamina",
                CloseButtonText = "确定"
            });
        }
    }
}