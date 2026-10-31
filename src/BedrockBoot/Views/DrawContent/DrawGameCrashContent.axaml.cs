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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Windows.Entity;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawGameCrashContent : UserControl
{
    private readonly GameCrashReportEventArgs _args = new();

    public DrawGameCrashContent()
    {
        InitializeComponent();
        SystemName.Text = $"{Environment.OSVersion.VersionString}";
        SystemArch.Text = RuntimeInformation.OSArchitecture.ToString();
        ExportBtn.Click += ExportBtn_OnClick;
    }

    public DrawGameCrashContent(GameCrashReportEventArgs args) : this()
    {
        _args = args ?? new GameCrashReportEventArgs();
        Fill();
    }

    /// <summary>
    /// 填充分析信息。实例配置可能取不到（注入进程、提权运行、目录异常等），
    /// 任何字段都做兜底，绝不能让 UI 构造抛异常——否则崩溃追踪界面永远不会出现。
    /// </summary>
    private void Fill()
    {
        var report = _args.Report;

        GameInstance.Text = string.IsNullOrEmpty(_args.GameName) ? "未知实例" : _args.GameName;

        VersionConfig? conf = null;
        try
        {
            if (!string.IsNullOrEmpty(_args.InstanceFolder))
                conf = GameInfoHelper.GetVersionConfig(_args.InstanceFolder);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取实例配置失败：{ex.Message}");
        }

        GameVersion.Text = conf?.Info?.Version ?? "未知";
        GameCorePath.Text = conf != null
            ? Path.Combine(conf.VersionPath, conf.BodyFile ?? string.Empty)
            : string.IsNullOrEmpty(_args.InstanceFolder)
                ? "未知"
                : _args.InstanceFolder;

        DumpFilePath.Text = File.Exists(_args.DumpFilePath) ? _args.DumpFilePath : "未生成转储文件";

        ExceptionName.Text = report?.ExceptionCodeName ?? "未捕获异常报告";
        ExceptionDesc.Text = report?.ExceptionDescription ?? "-";
        SourceFile.Text = FirstNonEmpty(report?.ExceptionModulePath, report?.Owner, "未知");
        ExceptionAddress.Text = report is { ExceptionAddress: > 0 } ? $"0x{report.ExceptionAddress:X}" : "-";
        ExitCode.Text = $"0x{_args.ExitCode:X8} ({_args.ExitCode})";
        PreLoadVersion.Text = FileVersionInfo.GetVersionInfo(Path.Combine(_args.InstanceFolder, "PreLoad.NET.dll")).FileVersion;
        Phase.Text = report?.Phase switch
        {
            "veh" => "VEH 向量异常捕获",
            "seh" => "SEH 顶层过滤捕获",
            "manual" => "手动捕获",
            null or "" => "-",
            var phase => phase
        };
        CaptureTime.Text = report is { CaptureTime: var time } && time != default
            ? time.ToString("yyyy-MM-dd HH:mm:ss.fff")
            : "-";

        NoReportTip.IsVisible = report == null;

        Stack.Text = report is not { StackTrace.Count: > 0 }
            ? "未能获取调用堆栈"
            : string.Join(Environment.NewLine, report.StackTrace.Select(FormatFrame));

        AnalysisText.Text = BuildAnalysis(report, _args.InstanceFolder);
    }

    private static string FormatFrame(GameDumpStackFrame x)
    {
        var head = $"0x{x.Address:X}";
        if (string.IsNullOrEmpty(x.ModulePath)) return head;
        if (string.IsNullOrEmpty(x.SymbolName)) return $"{head} {x.ModulePath}";
        var disp = x.SymbolDisplacement.HasValue ? $" +0x{x.SymbolDisplacement.Value:X}" : "";
        return $"{head} {x.ModulePath} | {x.SymbolName}{disp}";
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "-";
    }

    private static string BuildAnalysis(GameDumpAnalysis? report, string instanceFolder)
    {
        if (report == null)
            return "本次异常退出没有生成崩溃报告，无法进一步分析。\n" +
                   "若需要捕获原生异常，请在实例设置中开启“原生异常捕获”，再复现一次即可获得完整分析。";

        var lines = new List<string>();

        switch (report.ExceptionCode)
        {
            case 0xC0000005 or 0xC0000006 when report.AccessViolationTarget is 0 or < 0x10000:
                lines.Add("访问的目标地址接近 0，大概率是空指针引用，常见于资源未加载完成或模组与当前游戏版本不兼容。");
                break;
            case 0xC0000005 or 0xC0000006:
                lines.Add("发生了非法内存访问。请结合调用堆栈中游戏本体之外的模块（模组、注入运行库、驱动等）排查。");
                break;
            case 0xC00000FD:
                lines.Add("线程栈空间耗尽，常见于无限递归或栈上分配超大对象，可能与异常模组有关。");
                break;
            case 0xC0000409 or 0xC0000602:
                lines.Add("运行库快速失败：检测到不可恢复的内部错误，常见于注入运行库与游戏版本不匹配。");
                break;
            case 0x80000003:
                lines.Add("触发断点指令，可能是游戏内部的调试断言。");
                break;
        }

        // 归因：崩溃源若落在实例的模组目录（DLL 模组 / LeviLamina 模组）里，按模组给出处理建议
        var modFile = FindModFile(report.ExceptionModulePath, instanceFolder)
                      ?? FindModFile(
                          string.IsNullOrEmpty(report.Owner) || report.Owner == "unresolved"
                              ? null
                              : report.Owner, instanceFolder)
                      ?? FindModFile(report.StackTrace
                          .FirstOrDefault(f => !string.IsNullOrEmpty(f.ModulePath))?.ModulePath, instanceFolder);

        if (modFile != null)
        {
            lines.Add($"崩溃源 {Path.GetFileName(modFile)} 是本实例模组目录下的模组 DLL，本次崩溃大概率由该模组引起。");
            lines.Add("建议先在实例的模组管理中禁用该模组并复现验证；确认后检查它是否适配当前游戏版本，" +
                      "或向模组作者反馈并附上导出的崩溃报告。");
        }
        else
        {
            lines.Add($"调用堆栈中出现了第三方模块 {report.Owner}，优先排查与它相关的模组或运行库。");
            lines.Add("请优先尝试根据上方分析进行自修复；如无法解决，可点击右上角“导出崩溃报告”打包向他人求助。");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static readonly string[] ModDirectorySuffixes =
    {
        Path.Combine("config", "BedrockBoot2", "mods"),
        Path.Combine("config", "BedrockBoot2", "levilamina", "ll.mods")
    };

    /// <summary>
    /// 判断崩溃源模块是否属于实例的模组 DLL：优先按完整路径归入模组目录，
    /// 模块只解析出文件名（owner=xxx.dll）时再在两个模组目录里按文件名兜底匹配。
    /// </summary>
    private static string? FindModFile(string? modulePath, string instanceFolder)
    {
        if (string.IsNullOrEmpty(instanceFolder)) return null;

        var modDirs = ModDirectorySuffixes
            .Select(suffix => Path.GetFullPath(Path.Combine(instanceFolder, suffix)))
            .Where(Directory.Exists)
            .ToList();
        if (modDirs.Count == 0) return null;

        if (!string.IsNullOrEmpty(modulePath) && modulePath.IndexOf('<') < 0 && !modulePath.StartsWith("0x"))
            try
            {
                var full = Path.GetFullPath(modulePath);
                if (full.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    var hit = modDirs.FirstOrDefault(dir =>
                        full.StartsWith(dir, StringComparison.OrdinalIgnoreCase));
                    if (hit != null) return full;
                }
            }
            catch
            {
                // 非法路径按未匹配处理
            }

        var fileName = Path.GetFileName(modulePath);
        if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            return modDirs.SelectMany(dir => Directory.EnumerateFiles(dir, "*.dll"))
                .FirstOrDefault(file => string.Equals(Path.GetFileName(file), fileName,
                    StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    private async void ExportBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            var reportPath = _args.Report?.FilePath;
            if (!CrashReportExporter.HasAnything(reportPath, _args.DumpFilePath))
            {
                GlobalModel.MainWindow?.Notice.AddNotice(new NoticeInfo
                {
                    Title = "没有可导出的文件",
                    Message = "本次崩溃没有可用的报告或转储文件"
                });
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "导出崩溃报告",
                SuggestedFileName = (string.IsNullOrEmpty(reportPath)
                    ? $"crash-{_args.LaunchId}"
                    : Path.GetFileNameWithoutExtension(reportPath)) + ".zip",
                DefaultExtension = "zip",
                FileTypeChoices = [new FilePickerFileType("压缩包") { Patterns = ["*.zip"] }]
            });
            if (file == null) return;

            await using var stream = await file.OpenWriteAsync();
            var exported = CrashReportExporter.Pack(reportPath, _args.DumpFilePath, stream);

            GlobalModel.MainWindow?.Notice.AddNotice(new NoticeInfo
            {
                Title = "崩溃报告已导出",
                Message = $"已打包 {exported} 个文件：{file.Name}"
            });
        }
        catch (Exception ex)
        {
            GlobalModel.MainWindow?.Notice.AddNotice(new NoticeInfo
            {
                Title = "导出失败",
                Message = ex.Message
            });
        }
    }
}