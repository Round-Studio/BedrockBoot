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

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BedrockBoot.Standard.Entity.Game;

namespace BedrockBoot.Windows.Models.Helper.Gdk;

/// <summary>
/// PreLoad.NET 崩溃报告解析器（logs/crash-reports/crash-*.txt）。
/// 报告由游戏进程内的 NativeExceptionMonitor 生成：key=value 头部 +
/// [exception_parameters] / [registers] / [stack] / [details] 段落。
/// </summary>
public static partial class GameCrashReportParser
{
    [GeneratedRegex(@"^#(\d+)\s+0x([0-9A-Fa-f]+)\s+(.+?)(?:\s+\|\s+(.+))?$")]
    private static partial Regex StackFrameRegex();

    public static GameDumpAnalysis? Parse(string reportFilePath)
    {
        if (!File.Exists(reportFilePath)) return null;

        try
        {
            var analysis = new GameDumpAnalysis();
            var info = new FileInfo(reportFilePath);
            analysis.FilePath = reportFilePath;
            analysis.FileSizeBytes = info.Length;
            analysis.CreatedTime = info.CreationTime;

            var parameters = new List<ulong>();
            var registers = new Dictionary<string, ulong>();
            var details = new StringBuilder();
            var section = "";

            foreach (var raw in File.ReadAllLines(reportFilePath))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0) continue;

                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    section = line[1..^1];
                    continue;
                }

                if (section == "stack")
                {
                    var frame = ParseStackFrame(line);
                    if (frame != null) analysis.StackTrace.Add(frame);
                    continue;
                }

                if (section == "details")
                {
                    details.AppendLine(line);
                    continue;
                }

                var eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line[..eq].Trim();
                var value = line[(eq + 1)..].Trim();

                switch (section)
                {
                    case "":
                        ParseHeader(analysis, parameters, key, value);
                        break;
                    case "exception_parameters":
                        ParseExceptionParameter(analysis, parameters, key, value);
                        break;
                    case "registers":
                        ParseRegister(analysis, registers, key, value);
                        break;
                }
            }

            analysis.ExceptionParameters = parameters;
            analysis.Registers = registers;
            if (details.Length > 0) analysis.Details = details.ToString().TrimEnd();
            analysis.HasException = analysis.ExceptionCode != 0;
            analysis.ExceptionCodeName = DescribeExceptionCode(analysis.ExceptionCode);
            analysis.ExceptionDescription =
                DescribeException(analysis.ExceptionCode, analysis.AccessViolationOperation,
                    analysis.AccessViolationTarget);
            return analysis;
        }
        catch
        {
            return null;
        }
    }

    private static void ParseHeader(GameDumpAnalysis analysis, List<ulong> parameters, string key, string value)
    {
        switch (key)
        {
            case "timestamp":
                DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time);
                analysis.CaptureTime = time;
                break;
            case "phase":
                analysis.Phase = value;
                break;
            case "title":
                analysis.Title = value;
                break;
            case "process_id":
                uint.TryParse(value, out var pid);
                analysis.ProcessId = pid;
                break;
            case "thread_id":
                uint.TryParse(value, out var tid);
                analysis.CrashedThreadId = tid;
                break;
            case "exception_code":
                analysis.ExceptionCode = ParseHex32(value);
                break;
            case "exception_address":
                analysis.ExceptionAddress = ParseHex64(value);
                break;
            case "exception_module":
                analysis.ExceptionModulePath = value;
                break;
            case "owner":
                analysis.Owner = value;
                break;
            case "dump":
                analysis.DumpFilePath = value;
                break;
        }
    }

    private static void ParseExceptionParameter(GameDumpAnalysis analysis, List<ulong> parameters,
        string key, string value)
    {
        switch (key)
        {
            case "access_violation.operation":
                analysis.AccessViolationOperation = value;
                break;
            case "access_violation.target":
                analysis.AccessViolationTarget = ParseHex64(value);
                break;
            default:
                if (key.StartsWith("param["))
                    parameters.Add(ParseHex64(value));
                break;
        }
    }

    private static void ParseRegister(GameDumpAnalysis analysis, Dictionary<string, ulong> registers,
        string key, string value)
    {
        var number = ParseHex64(value);
        registers[key] = number;
        switch (key)
        {
            case "rip":
                analysis.InstructionPointer = number;
                break;
            case "rsp":
                analysis.StackPointer = number;
                break;
        }
    }

    private static GameDumpStackFrame? ParseStackFrame(string line)
    {
        var match = StackFrameRegex().Match(line);
        if (!match.Success) return null;

        var frame = new GameDumpStackFrame
        {
            Index = int.Parse(match.Groups[1].Value),
            Address = Convert.ToUInt64(match.Groups[2].Value, 16),
            ModulePath = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null
        };

        var symbol = match.Groups[4].Success ? match.Groups[4].Value.Trim() : null;
        if (!string.IsNullOrEmpty(symbol))
        {
            var split = symbol.LastIndexOf("+0x", StringComparison.OrdinalIgnoreCase);
            if (split > 0 && ulong.TryParse(symbol[(split + 3)..], NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out var displacement))
            {
                frame.SymbolName = symbol[..split];
                frame.SymbolDisplacement = displacement;
            }
            else
            {
                frame.SymbolName = symbol;
            }
        }

        return frame;
    }

    private static uint ParseHex32(string value) =>
        value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? Convert.ToUInt32(value[2..], 16)
            : uint.TryParse(value, out var number) ? number : 0;

    private static ulong ParseHex64(string value) =>
        value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? Convert.ToUInt64(value[2..], 16)
            : ulong.TryParse(value, out var number) ? number : 0;

    private static string DescribeExceptionCode(uint code) => code switch
    {
        0xC0000005 => "访问冲突 (EXCEPTION_ACCESS_VIOLATION)",
        0xC0000006 => "页内错误 (EXCEPTION_IN_PAGE_ERROR)",
        0x80000003 => "断点 (EXCEPTION_BREAKPOINT)",
        0x80000002 => "数据对齐错误 (EXCEPTION_DATATYPE_MISALIGNMENT)",
        0xC000001D => "非法指令 (EXCEPTION_ILLEGAL_INSTRUCTION)",
        0xC0000025 => "不可继续异常 (EXCEPTION_NONCONTINUABLE_EXCEPTION)",
        0xC0000094 => "整数除零 (EXCEPTION_INT_DIVIDE_BY_ZERO)",
        0xC0000096 => "特权指令 (EXCEPTION_PRIV_INSTRUCTION)",
        0xC00000FD => "栈溢出 (EXCEPTION_STACK_OVERFLOW)",
        0xC0000409 => "栈缓冲区溢出/快速失败 (STATUS_STACK_BUFFER_OVERRUN)",
        0xC000041D => "未处理异常 (EXCEPTION_INVALID_PARAMETER)",
        0xC0000602 => "快速失败 (FAIL_FAST_EXCEPTION)",
        0x40000015 => "应用主动退出 (STATUS_FATAL_APP_EXIT)",
        _ => $"未知异常 (0x{code:X8})"
    };

    private static string? DescribeException(uint code, string? operation, ulong? target)
    {
        switch (code)
        {
            case 0xC0000005 or 0xC0000006 when target != null:
            {
                var access = operation switch
                {
                    "read" => "读取",
                    "write" => "写入",
                    "execute" => "执行",
                    _ => "访问"
                };
                return $"{DescribeExceptionCode(code)}：尝试{access}地址 0x{target:X}";
            }
            case 0xC00000FD:
                return $"{DescribeExceptionCode(code)}：线程栈空间耗尽";
            default:
                return null;
        }
    }
}
