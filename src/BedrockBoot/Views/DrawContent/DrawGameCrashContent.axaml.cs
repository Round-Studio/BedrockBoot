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

        GameInstance.Text = string.IsNullOrEmpty(_args.GameName) ? I18nManager.Instance["CrashReport.Unknown.Instance"] : _args.GameName;

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

        GameVersion.Text = conf?.Info?.Version ?? I18nManager.Instance["CrashReport.Unknown"];
        GameCorePath.Text = conf != null
            ? Path.Combine(conf.VersionPath, conf.BodyFile ?? string.Empty)
            : string.IsNullOrEmpty(_args.InstanceFolder)
                ? I18nManager.Instance["CrashReport.Unknown"]
                : _args.InstanceFolder;

        DumpFilePath.Text = File.Exists(_args.DumpFilePath) ? _args.DumpFilePath : I18nManager.Instance["CrashReport.NoDumpFile"];

        ExceptionName.Text = report?.ExceptionCodeName ?? I18nManager.Instance["CrashReport.NoExceptionReport"];
        ExceptionDesc.Text = report?.ExceptionDescription ?? "-";
        SourceFile.Text = FirstNonEmpty(report?.ExceptionModulePath, report?.Owner, I18nManager.Instance["CrashReport.Unknown"]);
        ExceptionAddress.Text = report is { ExceptionAddress: > 0 } ? $"0x{report.ExceptionAddress:X}" : "-";
        ExitCode.Text = $"0x{_args.ExitCode:X8} ({_args.ExitCode})";
        PreLoadVersion.Text = GetFileVersion(Path.Combine(_args.InstanceFolder, "PreLoad.NET.dll"));
        Phase.Text = report?.Phase switch
        {
            "veh" => I18nManager.Instance["CrashReport.Phase.VEH"],
            "seh" => I18nManager.Instance["CrashReport.Phase.SEH"],
            "manual" => I18nManager.Instance["CrashReport.Phase.Manual"],
            null or "" => "-",
            var phase => phase
        };
        CaptureTime.Text = report is { CaptureTime: var time } && time != default
            ? time.ToString("yyyy-MM-dd HH:mm:ss.fff")
            : "-";

        NoReportTip.IsVisible = report == null;

        Stack.Text = report is not { StackTrace.Count: > 0 }
            ? I18nManager.Instance["CrashReport.NoStackTrace"]
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

    private static string GetFileVersion(string path)
    {
        try
        {
            return File.Exists(path) ? FileVersionInfo.GetVersionInfo(path).FileVersion ?? "-" : I18nManager.Instance["CrashReport.Unknown"];
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取文件版本信息失败：{ex.Message}");
            return I18nManager.Instance["CrashReport.Unknown"];
        }
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "-";
    }

    private static string BuildAnalysis(GameDumpAnalysis? report, string instanceFolder)
    {
        if (report == null)
            return I18nManager.Instance["CrashReport.Analysis.NoReport"];

        var lines = new List<string>();

        switch (report.ExceptionCode)
        {
            case 0xC0000005 or 0xC0000006 when report.AccessViolationTarget is 0 or < 0x10000:
                lines.Add(I18nManager.Instance["CrashReport.Analysis.NullPointer"]);
                break;
            case 0xC0000005 or 0xC0000006:
                lines.Add(I18nManager.Instance["CrashReport.Analysis.IllegalAccess"]);
                break;
            case 0xC00000FD:
                lines.Add(I18nManager.Instance["CrashReport.Analysis.StackOverflow"]);
                break;
            case 0xC0000409 or 0xC0000602:
                lines.Add(I18nManager.Instance["CrashReport.Analysis.FastFail"]);
                break;
            case 0x80000003:
                lines.Add(I18nManager.Instance["CrashReport.Analysis.Breakpoint"]);
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
            lines.Add(string.Format(I18nManager.Instance["CrashReport.Analysis.ModCrash"], Path.GetFileName(modFile)));
            lines.Add(I18nManager.Instance["CrashReport.Analysis.ModCrash.Suggestion2"]);
        }
        else
        {
            lines.Add(string.Format(I18nManager.Instance["CrashReport.Analysis.ThirdParty"], report.Owner));
            lines.Add(I18nManager.Instance["CrashReport.Analysis.Suggestion"]);
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
                    Title = I18nManager.Instance["CrashReport.Export.Empty.Title"],
                    Message = I18nManager.Instance["CrashReport.Export.Empty.Message"]
                });
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = I18nManager.Instance["CrashReport.Export.Title"],
                SuggestedFileName = (string.IsNullOrEmpty(reportPath)
                    ? $"crash-{_args.LaunchId}"
                    : Path.GetFileNameWithoutExtension(reportPath)) + ".zip",
                DefaultExtension = "zip",
                FileTypeChoices = [new FilePickerFileType(I18nManager.Instance["CrashReport.Export.FileType"]) { Patterns = ["*.zip"] }]
            });
            if (file == null) return;

            await using var stream = await file.OpenWriteAsync();
            var exported = CrashReportExporter.Pack(reportPath, _args.DumpFilePath, stream);

            GlobalModel.MainWindow?.Notice.AddNotice(new NoticeInfo
            {
                Title = I18nManager.Instance["CrashReport.Export.Success.Title"],
                Message = string.Format(I18nManager.Instance["CrashReport.Export.Success.Message"], exported, file.Name)
            });
        }
        catch (Exception ex)
        {
            GlobalModel.MainWindow?.Notice.AddNotice(new NoticeInfo
            {
                Title = I18nManager.Instance["CrashReport.Export.Failed.Title"],
                Message = ex.Message
            });
        }
    }
}