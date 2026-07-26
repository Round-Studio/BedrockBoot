using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Windows.Management.Deployment;

namespace BedrockBoot.Models.Helper.Gdk;

public class AppSdkChecker
{
    /// <summary>
    /// Windows App SDK 1.8 对应的运行时主版本号。
    /// Main / DDLM 组件的版本形如 8000.921.1539.0。
    /// </summary>
    private const int TargetMajorVersion = 8000;

    /// <summary>
    /// 所有 Windows App SDK 运行时包名中共有的标识。
    /// </summary>
    private const string RuntimeMarker = "WinAppRuntime";

    /// <summary>
    /// 最近一次检测的诊断信息，用于在提示框中展示缺失的组件。
    /// </summary>
    public static string LastDiagnostics { get; private set; } = string.Empty;

    public static bool GetInstalled()
    {
        // 方法 1: WinRT PackageManager (最快且最权威，无进程开销)
        // 返回 null 表示 API 不可用，需要继续尝试其他方式。
        var winRtResult = CheckViaWinRt();
        if (winRtResult.HasValue)
        {
            return winRtResult.Value;
        }

        // 方法 2: 文件系统检测 (无进程开销，但通常需要管理员权限)
        // 返回 null 表示无法确定 (例如目录不可访问)，此时必须继续走 PowerShell 检测。
        if (CheckViaFileSystem() == true)
        {
            return true;
        }

        // 方法 3: PowerShell 检测 (较慢，最终兜底)
        return CheckViaPowerShell();
    }

    /// <summary>
    /// 通过 WinRT PackageManager 枚举当前用户已安装的包。
    /// </summary>
    /// <returns>
    /// true/false 表示检测结论；null 表示 WinRT 不可用，调用方需回退到其他方式。
    /// </returns>
    private static bool? CheckViaWinRt()
    {
        try
        {
            var manager = new PackageManager();
            var state = new ComponentState();

            // 传入空字符串表示当前用户
            foreach (var package in manager.FindPackagesForUser(string.Empty))
            {
                var id = package.Id;
                if (id?.Name == null) continue;

                var v = id.Version;
                Accumulate(id.Name, $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}", state);
            }

            return Evaluate(state, "WinRT");
        }
        catch (Exception ex)
        {
            // 例如在非打包环境或受限系统上 API 不可用，交由后续方式处理。
            Console.WriteLine($@"WinRT check unavailable, falling back: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 判断单个包是否满足要求，并累计各组件的命中情况。
    /// </summary>
    /// <remarks>
    /// Singleton 包是跨版本共享的单例组件，微软对其采用独立的版本号策略
    /// (例如 SDK 1.8 环境下实际版本为 8002.3.0.0)，因此不能用 8000.x 去校验它，
    /// 否则会把已装好的 SDK 1.8 误判为未安装。
    /// </remarks>
    private static void Accumulate(string packageName, string packageVersion, ComponentState state)
    {
        if (string.IsNullOrEmpty(packageName) ||
            packageName.IndexOf(RuntimeMarker, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return;
        }

        // Singleton: 仅校验包名，不校验版本。
        if (packageName.IndexOf("Singleton", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            state.HasSingleton = true;
            return;
        }

        // Main / DDLM: 使用语义化版本比较，主版本号需 >= 8000。
        // 这样未来的 8001.x / 8003.x 等版本线也不会被误判。
        if (!IsVersionSatisfied(packageVersion))
        {
            return;
        }

        if (packageName.IndexOf("Main", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            state.HasMain = true;
        }

        if (packageName.IndexOf("DDLM", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            state.HasDdlm = true;
        }
    }

    private static bool IsVersionSatisfied(string packageVersion)
    {
        return Version.TryParse(packageVersion, out var version) && version.Major >= TargetMajorVersion;
    }

    private static bool Evaluate(ComponentState state, string source)
    {
        if (state.IsComplete)
        {
            LastDiagnostics = string.Empty;
            Console.WriteLine($@"{source} check passed (Main + Singleton + DDLM).");
            return true;
        }

        LastDiagnostics = state.DescribeMissing();
        Console.WriteLine(
            $@"{source} missing components: Main={state.HasMain}, Singleton={state.HasSingleton}, DDLM={state.HasDdlm}");
        return false;
    }

    private static bool CheckViaPowerShell()
    {
        // 只负责取回原始数据，判定逻辑统一放在 C# 侧，避免两处规则不一致。
        const string script =
            "Get-AppxPackage | " +
            "Where-Object { $_.Name -like '*WinAppRuntime*' } | " +
            "ForEach-Object { $_.Name + '|' + $_.Version }";

        // 使用 -EncodedCommand 传参，彻底规避引号与 $ 符号的转义问题。
        var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                LastDiagnostics = "无法启动 PowerShell 进程。";
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine($@"PS Error: {error.Trim()}");
            }

            var state = new ComponentState();

            foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                var separatorIndex = line.LastIndexOf('|');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var name = line.Substring(0, separatorIndex);
                var version = line.Substring(separatorIndex + 1);
                Accumulate(name, version, state);
            }

            return Evaluate(state, "PowerShell");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PS Exception: {ex.Message}");
            LastDiagnostics = $"检测过程出错: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// 通过扫描 WindowsApps 目录检测。
    /// </summary>
    /// <returns>
    /// true 表示确认已安装；false 表示确认未安装；
    /// null 表示无法确定 (目录不存在或无访问权限)，调用方需改用其他方式检测。
    /// </returns>
    private static bool? CheckViaFileSystem()
    {
        string windowsAppsPath;
        string[] directories;

        try
        {
            windowsAppsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");

            if (!Directory.Exists(windowsAppsPath))
            {
                return null;
            }

            directories = Directory.GetDirectories(windowsAppsPath);
        }
        catch (UnauthorizedAccessException)
        {
            // WindowsApps 默认拒绝普通用户访问，这属于「无法确定」而非「未安装」。
            Console.WriteLine(@"FS check skipped: access to WindowsApps denied.");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"FS check skipped: {ex.Message}");
            return null;
        }

        var state = new ComponentState();

        foreach (var directory in directories)
        {
            var directoryName = Path.GetFileName(directory);
            if (string.IsNullOrEmpty(directoryName))
            {
                continue;
            }

            // 目录名格式: <包名>_<版本>_<架构>__<发布者哈希>
            var segments = directoryName.Split('_');
            if (segments.Length < 2)
            {
                continue;
            }

            Accumulate(segments[0], segments[1], state);
        }

        return Evaluate(state, "File system") ? true : false;
    }

    public static async Task<bool> ShowNotice()
    {
        var tcs = new TaskCompletionSource<bool>();
        var missing = string.IsNullOrEmpty(LastDiagnostics)
            ? "缺失组件可能包括: Main, Singleton 或 DDLM。"
            : LastDiagnostics;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var dialogInfo = new DialogInfo
            {
                Title = "未安装 SDK 1.8",
                Content = "当前系统未检测到完整的 Windows App SDK 1.8 运行时组件。\n" +
                          missing + "\n" +
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

    private sealed class ComponentState
    {
        public bool HasMain { get; set; }
        public bool HasSingleton { get; set; }
        public bool HasDdlm { get; set; }

        public bool IsComplete => HasMain && HasSingleton && HasDdlm;

        public string DescribeMissing()
        {
            var missing = new List<string>();
            if (!HasMain) missing.Add("Main");
            if (!HasSingleton) missing.Add("Singleton");
            if (!HasDdlm) missing.Add("DDLM");

            return missing.Count == 0
                ? string.Empty
                : $"缺失组件: {string.Join("、", missing)}。";
        }
    }
}
