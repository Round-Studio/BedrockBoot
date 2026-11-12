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
using System.Threading.Tasks;
using Avalonia;
using BedrockBoot;
using Avalonia.Threading;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Windows.Models.Helper;

public class DeveloperModeHelper
{
    public enum DeveloperModeCheckStatus
    {
        Enabled,
        Disabled,
        Failed
    }

    public enum DeveloperModeNoticeResult
    {
        Dismissed,
        OpenSettings,
        Ignore
    }

    public readonly record struct DeveloperModeCheckResult(DeveloperModeCheckStatus Status, int? ExitCode);

    public static DeveloperModeCheckResult IsDeveloperModeViaPowerShell()
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
                if (process == null)
                    return new DeveloperModeCheckResult(DeveloperModeCheckStatus.Failed, null);

                var outputTask = process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                string output = outputTask.GetAwaiter().GetResult().Trim();
                int exitCode = process.ExitCode;
                var status = exitCode == 0 && output.Equals("true", StringComparison.OrdinalIgnoreCase)
                    ? DeveloperModeCheckStatus.Enabled
                    : exitCode == 0 && output.Equals("false", StringComparison.OrdinalIgnoreCase)
                        ? DeveloperModeCheckStatus.Disabled
                        : DeveloperModeCheckStatus.Failed;

                Console.WriteLine($@"Pwsh 标准输出：{output}");
                Console.WriteLine($@"Pwsh 退出代码：{exitCode}");
                Console.WriteLine($@"Pwsh 检测状态：{status}");

                return new DeveloperModeCheckResult(status, exitCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PowerShell 进程调用失败: {ex}");
            return new DeveloperModeCheckResult(DeveloperModeCheckStatus.Failed, null);
        }
    }

    public static Task<DeveloperModeNoticeResult> ShowNotice(bool checkFailed = false)
    {
        var completion = new TaskCompletionSource<DeveloperModeNoticeResult>(TaskCreationOptions.RunContinuationsAsynchronously);


        Dispatcher.UIThread.Invoke(() => DialogHost.Show(checkFailed
            ? new DialogInfo
            {
                Title = "检查 Windows 开发人员模式失败",
                Content = "无法自动确认 Windows 开发人员模式状态。\n未开启 Windows 开发人员模式无法安装或启动 UWP 类型构建的 Minecraft\n\n请手动检查 Windows 设置中的“开发人员模式”是否开启",
                SecondaryButtonText = "手动检查",
                SecondaryAction = () =>
                {
                    completion.TrySetResult(DeveloperModeNoticeResult.OpenSettings);
                    OpenDeveloperSettings();
                },
                CloseButtonText = "确定",
                CloseAction = () => completion.TrySetResult(DeveloperModeNoticeResult.Dismissed),
                AccountButton = DialogButtons.CloseButton
            }
            : new DialogInfo
            {
                Title = "未开启开发人员模式",
                Content = "当前系统未开启开发人员模式，无法安装或启动 UWP 类型构建的 Minecraft。\n\n请前往 Windows 设置中的“开发人员模式”开启。",
                SecondaryButtonText = "忽略",
                SecondaryAction = () => completion.TrySetResult(DeveloperModeNoticeResult.Ignore),
                CloseButtonText = "确定",
                CloseAction = () =>
                {
                    completion.TrySetResult(DeveloperModeNoticeResult.OpenSettings);
                    OpenDeveloperSettings();
                },
                AccountButton = DialogButtons.CloseButton
            }));

        return completion.Task;
    }

    private static void OpenDeveloperSettings()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "ms-settings:developers",
            UseShellExecute = true
        });
    }
}