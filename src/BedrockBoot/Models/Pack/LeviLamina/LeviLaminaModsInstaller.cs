using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Loader.LeviLamina;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Models.Pack.LeviLamina;

public class LeviLaminaModsInstaller
{
    private readonly PackageInfo _pkg;
    private readonly string _key;

    public LeviLaminaModsInstaller(PackageInfo pkg, string key)
    {
        _pkg = pkg;
        _key = key;
    }

    public async Task Install(string versionId, string savePath, bool isOnlyDownload = false)
    {
        if (!isOnlyDownload)
        {
            var dialogContent = new DialogInstallLeviLaminaModContent(versionId, savePath, _key, isOnlyDownload);

            DialogHost.Show(new DialogInfo
            {
                Title = $"正在安装 LeviLamina v{versionId}",
                Content = dialogContent
            });
        }
    }
}