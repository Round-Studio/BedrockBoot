using System.Diagnostics;
using System.Runtime.Versioning;
using Avalonia.Threading;
using Microsoft.Win32;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Helper;

public class DeveloperModeHelper
{
    private const string ValueName = "AllowDevelopmentWithoutDevLicense";

    /// <summary>
    /// 开发者模式对应的注册表位置，与原 PowerShell 脚本检查的路径一致
    /// </summary>
    private static readonly string[] RegistryPaths =
    {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock",
        @"SOFTWARE\Policies\Microsoft\Windows\Appx"
    };

    /// <summary>
    /// 检测系统是否开启了开发者模式。
    /// 直接读取注册表，避免为了两个值而启动一个 PowerShell 进程（约 800ms）。
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static bool IsDeveloperMode()
    {
        foreach (var path in RegistryPaths)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(path);
                // 该值为 DWORD，1 表示已开启
                if (key?.GetValue(ValueName) is int value && value == 1) return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"读取开发者模式注册表 {path} 失败: {ex.Message}");
            }
        }

        return false;
    }

    /// <summary>
    /// 兼容旧调用点的别名。
    /// </summary>
    [SupportedOSPlatform("windows")]
    [Obsolete("已改为直接读取注册表，请调用 IsDeveloperMode()")]
    public static bool IsDeveloperModeViaPowerShell() => IsDeveloperMode();

    public static void ShowNotice()
    {
        Dispatcher.UIThread.Invoke(() => DialogHost.Show(new DialogInfo
        {
            Title = "未开启开发者模式",
            Content = "当前系统未开启管理员模式，\n" +
                      "需要前往 系统设置>高级 中开启开发者模式",
            CloseButtonText = "打开系统高级设置",
            CloseAction = () =>
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "ms-settings:developers",
                    UseShellExecute = true
                });
            }
        }));
    }
}