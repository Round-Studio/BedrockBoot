using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Helper.Gdk;

public class AppSdkChecker
{
    // 目标版本前缀
    private const string TargetVersionPrefix = "8000.";
    
    public static bool GetInstalled()
    {
        // 方法 1: PowerShell 检测 (主要手段，最准确)
        if (CheckViaPowerShell())
        {
            return true;
        }

        // 方法 2: 文件系统检测 (备用手段)
        if (CheckViaFileSystem())
        {
            return true;
        }

        return false;
    }

    private static bool CheckViaPowerShell()
    {
        var command = @"
        $packages = Get-AppxPackage | Where-Object { $_.Version -like '8000.*' -and $_.Name -like '*WinAppRuntime*' };
        
        $hasMain = $false;
        $hasSingleton = $false;
        $hasDDLM = $false;

        foreach ($pkg in $packages) {
            $name = $pkg.Name;
            if ($name -like '*Main*') { $hasMain = $true; }
            if ($name -like '*Singleton*') { $hasSingleton = $true; }
            # DDLM 的包名通常是 Microsoft.WinAppRuntime.DDLM...
            if ($name -like '*DDLM*') { $hasDDLM = $true; }
        }

        if ($hasMain -and $hasSingleton -and $hasDDLM) { 
            Write-Output 'TRUE' 
        } else { 
            Write-Output 'FALSE' 
        }
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

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // 调试输出
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($@"PS Error: {error.Trim()}");
                }
                
                string trimmedOutput = output.Trim();
                Console.WriteLine($@"PS Output: '{trimmedOutput}'");

                // 只要输出中包含 TRUE 即视为成功
                return trimmedOutput.IndexOf("TRUE", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PS Exception: {ex.Message}");
            return false;
        }
    }

    private static bool CheckViaFileSystem()
    {
        try
        {
            string windowsAppsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
            
            if (!Directory.Exists(windowsAppsPath)) return false;

            var dirs = Directory.GetDirectories(windowsAppsPath);
            
            bool hasMain = false;
            bool hasSingleton = false;
            bool hasDDLM = false;

            foreach (var dir in dirs)
            {
                string dirName = Path.GetFileName(dir);
                
                // 检查目录名是否包含 WinAppRuntime 且包含版本号 8000
                if (dirName.Contains("WinAppRuntime") && dirName.Contains("8000."))
                {
                    if (dirName.Contains("Main")) hasMain = true;
                    if (dirName.Contains("Singleton")) hasSingleton = true;
                    if (dirName.Contains("DDLM")) hasDDLM = true;
                }
            }

            // 必须三个组件都存在
            if (hasMain && hasSingleton && hasDDLM)
            {
                Console.WriteLine(@"File system check passed (Main + Singleton + DDLM).");
                return true;
            }
            else
            {
                Console.WriteLine($@"FS Missing components: Main={hasMain}, Singleton={hasSingleton}, DDLM={hasDDLM}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"FS Exception: {ex.Message}");
        }

        return false;
    }

    public static async Task<bool> ShowNotice()
    {
        var tcs = new TaskCompletionSource<bool>();

        await Dispatcher.UIThread.InvokeAsync(() =>
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
                    tcs.TrySetResult(true);
                },
            };
            DialogHost.Show(dialogInfo);
        });

        return await tcs.Task;
    }
}