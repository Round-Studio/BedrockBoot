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
using System.Runtime.InteropServices;
using BedrockBoot.Windows.Entity;

namespace BedrockBoot.Windows.Models.Helper.Gdk;

/// <summary>
/// PreLoad.NET 崩溃报告监视器。
/// 游戏进程内的 NativeExceptionMonitor（需实例配置开启 isNativeException）会在
/// 崩溃时把报告（crash-*.txt）与转储（crash-*.dmp）写入游戏目录的
/// logs/crash-reports/。本类在游戏运行期间被动监视该目录，进程退出后选取
/// 本次令进程崩溃的那个转储（最后一份带 dump 的报告），解析为 GameDumpAnalysis
/// 实体并触发回调。
/// </summary>
public static class GameCrashReportWatcher
{
    public static event Action<GameCrashReportEventArgs>? CrashReportGenerated;

    private const uint SYNCHRONIZE = 0x00100000;
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x00001000;
    private const uint INFINITE = 0xFFFFFFFF;

    public static string BeginCapture(Process gameProcess, string crashReportDirectory, string gameName)
    {
        var launchId = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..4]}";

        Task.Factory.StartNew(
            () => WatchLoop(gameProcess, crashReportDirectory, gameName, launchId),
            TaskCreationOptions.LongRunning);

        return launchId;
    }

    private static void WatchLoop(Process gameProcess, string crashReportDirectory, string gameName,
        string launchId)
    {
        try
        {
            WatchLoopCore(gameProcess, crashReportDirectory, gameName, launchId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"崩溃报告监视线程异常退出：{ex}");
        }
    }

    private static void WatchLoopCore(Process gameProcess, string crashReportDirectory, string gameName,
        string launchId)
    {
        // 提权运行或进程受限时 MainModule 访问会抛异常，这里只影响展示用的实例目录，不能让监视线程挂掉
        string? instanceFolder = null;
        try
        {
            instanceFolder = Path.GetDirectoryName(gameProcess.MainModule?.FileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取游戏主模块路径失败：{ex.Message}");
        }

        var processId = gameProcess.Id;
        var processHandle = OpenProcess(SYNCHRONIZE | PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)processId);
        if (processHandle == IntPtr.Zero)
            Console.WriteLine(
                $@"打开游戏进程 {processId} 失败（Win32 错误码 {Marshal.GetLastWin32Error()}），改用托管方式等待退出");

        var knownReports = SnapshotReports(crashReportDirectory);
        Console.WriteLine($@"已开始监视游戏进程 {processId} 的崩溃报告（启动标识 {launchId}）");

        var exitCode = 0u;
        try
        {
            if (processHandle != IntPtr.Zero)
            {
                WaitForSingleObject(processHandle, INFINITE);
                if (GetExitCodeProcess(processHandle, out exitCode) && exitCode != 0)
                    Console.WriteLine($@"游戏异常退出，退出码：0x{exitCode:X8}");
            }
            else
            {
                gameProcess.WaitForExit();
                try
                {
                    exitCode = unchecked((uint)gameProcess.ExitCode);
                    if (exitCode != 0) Console.WriteLine($@"游戏异常退出，退出码：0x{exitCode:X8}");
                }
                catch
                {
                }
            }
        }
        finally
        {
            if (processHandle != IntPtr.Zero) CloseHandle(processHandle);
        }

        Console.WriteLine(@"游戏已退出，停止崩溃报告监视");

        var reports = new List<GameDumpAnalysis>();
        foreach (var reportPath in EnumerateNewReports(crashReportDirectory, knownReports))
        {
            var analysis = GameCrashReportParser.Parse(reportPath);
            if (analysis != null) reports.Add(analysis);
        }

        // 只取本次令进程崩溃的那一个：最后一份带转储的报告（中间被 SEH 救回的捕获不带或较早）
        var fatal = reports
            .Where(r => !string.IsNullOrEmpty(r.DumpFilePath))
            .OrderBy(r => r.CaptureTime)
            .LastOrDefault();

        if (fatal != null)
            Console.WriteLine($@"已定位崩溃转储：{fatal.DumpFilePath}");

        if (fatal == null && exitCode == 0) return;

        try
        {
            CrashReportGenerated?.Invoke(new GameCrashReportEventArgs
            {
                InstanceFolder = instanceFolder,
                LaunchId = launchId,
                GameName = gameName,
                ExitCode = exitCode,
                DumpFilePath = fatal?.DumpFilePath,
                Report = fatal
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"崩溃报告回调执行失败：{ex}");
        }
    }

    private static HashSet<string> SnapshotReports(string directory) =>
        Directory.Exists(directory)
            ? Directory.GetFiles(directory, "crash-*.txt").ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];

    private static IEnumerable<string> EnumerateNewReports(string directory, HashSet<string> known) =>
        Directory.Exists(directory)
            ? Directory.GetFiles(directory, "crash-*.txt").Where(path => !known.Contains(path))
            : [];

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
