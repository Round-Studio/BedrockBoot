/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Diagnostics;
using System.Text;
using Avalonia.Threading;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Helper;

public class DeveloperModeHelper
{
    public static bool IsDeveloperModeViaPowerShell()
    {
        string script = @"
            $paths = @(
                'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock',
                'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Appx'
            )
            $valueName = 'AllowDevelopmentWithoutDevLicense'
            
            foreach ($path in $paths) {
                try {
                    $value = Get-ItemProperty -Path $path -Name $valueName -ErrorAction Stop
                    if ($value.$valueName -eq 1) {
                        Write-Output 'true'
                        return
                    }
                } catch {
                    continue
                }
            }
            Write-Output 'false'
        ";
        
        // 将脚本编码为 Base64（避免命令行转义问题）
        byte[] scriptBytes = Encoding.Unicode.GetBytes(script);
        string base64Script = Convert.ToBase64String(scriptBytes);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {base64Script}",
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
                process.WaitForExit();

                var result = output.Equals("true", StringComparison.OrdinalIgnoreCase);
                Console.WriteLine($@"Pwsh 调用输出：{result}");
                
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PowerShell 进程调用失败: {ex}");
            return false;
        }
    }

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