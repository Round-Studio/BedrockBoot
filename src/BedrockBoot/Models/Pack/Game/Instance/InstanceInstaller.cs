using BedrockBoot.Base.Entry;
using BedrockBoot.Views.DialogContent.Install;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Pack.Game.Instance;

public class InstanceInstaller
{
    private readonly BuildInfo _info;

    public InstanceInstaller(BuildInfo info)
    {
        _info = info;
    }

    public void Install()
    {
        var dialog = new DialogGameInstallInfoContent(_info);
        DialogHost.Show(new DialogInfo()
        {
            Content = dialog,
            Title = "安装实例",
            CloseButtonText = "安装",
            PrimaryButtonText = "取消",
            CloseAction = () => dialog.ExecuteInstallTask()
        });
    }
}