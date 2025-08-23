using BedrockBoot.Models.Enum.Background;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;

namespace BedrockBoot.Models.Classes.Style.Background;

public class BackgroundManager
{
    public static void UpdateBackground()
    {
        // global_cfg.MainWindow.SystemBackdrop = new TransparentBackdrop();
        switch (global_cfg.cfg.JsonCfg.BackgroundEnum)
        {
            case BackgroundEnum.None:
                global_cfg.MainWindow.SystemBackdrop = null;
                break;
            case BackgroundEnum.Mica:
                global_cfg.MainWindow.SystemBackdrop = new DevWinUI.MicaSystemBackdrop();
                break;
            case BackgroundEnum.BaseAlt:
                global_cfg.MainWindow.SystemBackdrop = new DevWinUI.MicaSystemBackdrop(MicaKind.BaseAlt);
                break;
            case BackgroundEnum.Acrylic:
                global_cfg.MainWindow.SystemBackdrop = new DevWinUI.AcrylicSystemBackdrop();
                break;
        }
    }
}