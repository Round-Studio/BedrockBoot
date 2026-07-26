using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Game;

namespace BedrockBoot.Core.Models.Helper;

/// <summary>
/// 自定义启动命令（启动前命令、启动后命令、运行包装器）的执行助手。
/// Windows 使用 cmd.exe /C 执行，Linux 使用 /bin/sh -c 执行。
/// </summary>
public static class LaunchCommandHelper
{
    /// <summary>包装器中代表原始游戏启动命令的占位符</summary>
    public const string CommandPlaceholder = "%command%";

    /// <summary>
    /// 将命令中的占位符替换为实例的实际信息。
    /// 支持：{instance_path} {instance_name} {game_version} {build_type} {body_file} {game_exe}
    /// </summary>
    public static string ExpandPlaceholders(string command, VersionConfig? versionInfo)
    {
        if (string.IsNullOrWhiteSpace(command)) return string.Empty;
        if (versionInfo == null) return command;

        var instancePath = versionInfo.VersionPath ?? string.Empty;
        var bodyFile = versionInfo.BodyFile ?? string.Empty;
        var gameExe = string.IsNullOrEmpty(instancePath) || string.IsNullOrEmpty(bodyFile)
            ? string.Empty
            : Path.Combine(instancePath, bodyFile);

        return command
            .Replace("{instance_path}", instancePath)
            .Replace("{instance_name}", versionInfo.Info?.VersionName ?? string.Empty)
            .Replace("{game_version}", versionInfo.Info?.Version ?? string.Empty)
            .Replace("{build_type}", versionInfo.Info?.BuildType.ToString() ?? string.Empty)
            .Replace("{body_file}", bodyFile)
            .Replace("{game_exe}", gameExe);
    }

    /// <summary>
    /// 执行一条自定义 Hook 命令（启动前 / 启动后）。
    /// </summary>
    /// <param name="command">要执行的命令行内容</param>
    /// <param name="versionInfo">用于展开占位符的实例信息，可为 null</param>
    /// <param name="waitForExit">是否等待命令执行结束</param>
    /// <param name="timeoutSeconds">等待超时时间（秒），小于等于 0 表示无限等待</param>
    /// <param name="label">日志标签，例如 "启动前命令"</param>
    /// <returns>命令的退出码；未等待或执行失败时返回 null</returns>
    public static async Task<int?> RunHookAsync(
        string command,
        VersionConfig? versionInfo,
        bool waitForExit,
        int timeoutSeconds,
        string label)
    {
        var expanded = ExpandPlaceholders(command, versionInfo);
        if (string.IsNullOrWhiteSpace(expanded)) return null;

        Console.WriteLine($@"[{label}] 准备执行: {expanded}");

        try
        {
            var startInfo = CreateShellStartInfo(expanded);

            // 工作目录设为实例目录，方便用户编写相对路径脚本
            if (!string.IsNullOrEmpty(versionInfo?.VersionPath) && Directory.Exists(versionInfo.VersionPath))
                startInfo.WorkingDirectory = versionInfo.VersionPath;

            using var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) Console.WriteLine($@"[{label}] {e.Data}");
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) Console.WriteLine($@"[{label}] {e.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!waitForExit)
            {
                Console.WriteLine($@"[{label}] 已在后台启动，不等待其结束");
                return null;
            }

            if (timeoutSeconds > 0)
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($@"[{label}] 执行超时（{timeoutSeconds} 秒），正在终止该命令");
                    TryKill(process);
                    return null;
                }
            }
            else
            {
                await process.WaitForExitAsync();
            }

            Console.WriteLine($@"[{label}] 执行结束，退出码: {process.ExitCode}");
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[{label}] 执行失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 使用运行包装器改写游戏进程的启动信息。
    /// 包装器中的 %command% 会被替换为原始的 文件名 + 参数。
    /// </summary>
    /// <returns>包装成功返回 true；包装器为空或解析失败时返回 false（此时不修改 startInfo）</returns>
    public static bool TryApplyWrapper(
        ProcessStartInfo startInfo,
        string wrapperCommand,
        VersionConfig? versionInfo)
    {
        var wrapper = ExpandPlaceholders(wrapperCommand, versionInfo);
        if (string.IsNullOrWhiteSpace(wrapper)) return false;

        if (!wrapper.Contains(CommandPlaceholder, StringComparison.Ordinal))
        {
            // 未提供占位符时，视为前缀包装器，自动追加原始命令
            wrapper = $"{wrapper.Trim()} {CommandPlaceholder}";
        }

        var originalCommand = string.IsNullOrEmpty(startInfo.Arguments)
            ? Quote(startInfo.FileName)
            : $"{Quote(startInfo.FileName)} {startInfo.Arguments}";

        var full = wrapper.Replace(CommandPlaceholder, originalCommand);

        var tokens = TokenizeCommandLine(full);
        if (tokens.Count == 0)
        {
            Console.WriteLine(@"[运行包装器] 无法解析包装器命令，已回退至原始启动方式");
            return false;
        }

        startInfo.FileName = tokens[0];
        startInfo.Arguments = tokens.Count > 1 ? string.Join(" ", tokens.GetRange(1, tokens.Count - 1)) : string.Empty;

        Console.WriteLine($@"[运行包装器] 已应用: {startInfo.FileName} {startInfo.Arguments}");
        return true;
    }

    /// <summary>创建平台对应的 Shell 启动信息</summary>
    private static ProcessStartInfo CreateShellStartInfo(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C {command}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
        }

        return new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = $"-c {Quote(command)}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"终止命令进程失败: {ex.Message}");
        }
    }

    /// <summary>为包含空格的路径补上引号</summary>
    private static string Quote(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.StartsWith('"') && value.EndsWith('"') && value.Length > 1) return value;
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    /// <summary>
    /// 按 Shell 规则切分命令行，保留引号内的整体性。
    /// 切分后的分段若原本带引号会重新补回引号，以便重新拼接为参数字符串。
    /// </summary>
    private static List<string> TokenizeCommandLine(string commandLine)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuote = false;
        var quoteChar = '\0';
        var wasQuoted = false;

        foreach (var c in commandLine)
        {
            if (inQuote)
            {
                if (c == quoteChar)
                {
                    inQuote = false;
                    continue;
                }

                current.Append(c);
                continue;
            }

            if (c is '"' or '\'')
            {
                inQuote = true;
                quoteChar = c;
                wasQuoted = true;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0 || wasQuoted)
                {
                    result.Add(Finalize(current.ToString(), wasQuoted, result.Count));
                    current.Clear();
                    wasQuoted = false;
                }

                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0 || wasQuoted)
            result.Add(Finalize(current.ToString(), wasQuoted, result.Count));

        return result;

        // 第一个分段作为 FileName 不需要引号，其余分段若含空格则补回引号
        static string Finalize(string token, bool quoted, int index)
        {
            if (index == 0) return token;
            return quoted || token.Contains(' ') ? $"\"{token}\"" : token;
        }
    }
}
