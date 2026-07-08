using System.Diagnostics;
using Windows.Management.Deployment;
using Avalonia.Threading;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Helper.Gdk;

public class AppSdkChecker
{
    public static bool GetInstalled()
    {
        var command = @"
        $p = @('MicrosoftCorporationII.WinAppRuntime.Main', 'MicrosoftCorporationII.WinAppRuntime.Singleton', 'Microsoft.WinAppRuntime.DDLM');
        $r = $true;
        foreach ($i in $p) {
            if (-not (Get-AppxPackage -Name ""$i*"" | Where-Object { $_.Version.StartsWith('8000.') })) {
                $r = $false;
                break;
            }
        };
        $r
    ";

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    
        try
        {
            using (var process = Process.Start(startInfo))
            {
                if (process == null) return false;
            
                string output = process.StandardOutput.ReadToEnd().Trim();
                string error = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();
            
                // 关键：输出错误信息
                Console.WriteLine($"Output: '{output}'");
                Console.WriteLine($"Error: '{error}'");
                Console.WriteLine($"ExitCode: {process.ExitCode}");
            
                return output.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PowerShell 进程调用失败: {ex}");
            return false;
        }
    }

    public static async Task<bool> ShowNotice()
    {
        var tcs = new TaskCompletionSource<bool>();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var dialogInfo = new DialogInfo
            {
                Title = "未安装 SDK 1.8",
                Content = "当前系统未安装 Windows App SDK 1.8 组件，\n" +
                          "可能会导致游戏无法启动或发生非预期行为。",
                CloseButtonText = "立即安装",
                PrimaryButtonText = "放任不管",
                AccountButton = DialogButtons.CloseButton,
                
                CloseAction = () =>
                {
                    tcs.TrySetResult(true);
                },
            };
            DialogHost.Show(dialogInfo);
        });

        // 等待用户交互完成
        return await tcs.Task;
    }
}