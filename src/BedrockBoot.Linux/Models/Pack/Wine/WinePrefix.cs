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
using BedrockBoot.Models.Global;
using BedrockBoot.Proton;

namespace BedrockBoot.Models.Pack.Wine;

public static class WinePrefix
{
    public static bool IsReady(string prefix)
    {
        var sys32 = Path.Combine(prefix, "drive_c", "windows", "system32");
        if (!Directory.Exists(sys32)) return false;
        foreach (var name in new[] { "system.reg", "user.reg" })
        {
            var path = Path.Combine(prefix, name);
            if (!File.Exists(path)) return false;
            var header = new byte[64];
            try
            {
                using var fs = File.OpenRead(path);
                fs.Read(header, 0, Math.Min(64, (int)fs.Length));
                if (System.Text.Encoding.UTF8.GetString(header).TrimStart('\0').StartsWith("WINE REGISTRY Version ")) continue;
                return false;
            }
            catch { return false; }
        }
        return true;
    }

    public static bool Boot()
    {
        var pfx = PathsList.WinePrefixPath;
        if (IsReady(pfx))
        {
            Console.WriteLine("Wine prefix ready");
            return true;
        }

        Console.WriteLine("Initialising Wine prefix...");
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(PathsList.NeoProtonPath, "proton", "GDK-Proton-xuser", "proton"),
            Arguments = "run wineboot -u",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.EnvironmentVariables["WINEPREFIX"] = pfx;
        // proton 脚本要求 STEAM_COMPAT_DATA_PATH（前缀会被解析为 <它>/pfx），
        // 以及 STEAM_COMPAT_CLIENT_INSTALL_PATH；缺任一个都会直接报错退出。
        psi.EnvironmentVariables["STEAM_COMPAT_DATA_PATH"] = PathsList.PreFixPath;
        psi.EnvironmentVariables["STEAM_COMPAT_CLIENT_INSTALL_PATH"] =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam");
        psi.EnvironmentVariables["WINEDEBUG"] = "-all";
        psi.EnvironmentVariables["SDL_VIDEODRIVER"] = "dummy";

        var proc = Process.Start(psi);
        if (proc == null) { Console.WriteLine("Failed to start wineboot"); return false; }

        if (!proc.WaitForExit(300000)) // 5 min timeout
        {
            Console.WriteLine("wineboot timed out");
            KillPrefixProcesses(pfx);
            return false;
        }

        if (proc.ExitCode != 0)
        {
            Console.WriteLine($"wineboot exited with code {proc.ExitCode}");
            KillPrefixProcesses(pfx);
            return false;
        }

        KillPrefixProcesses(pfx);

        var deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 30;
        while (DateTimeOffset.UtcNow.ToUnixTimeSeconds() < deadline && !IsReady(pfx))
            Thread.Sleep(1000);

        return IsReady(pfx);
    }

    public static void KillPrefixProcesses(string prefix)
    {
        foreach (var procDir in Directory.GetDirectories("/proc"))
        {
            var pidStr = Path.GetFileName(procDir);
            if (!int.TryParse(pidStr, out var pid) || pid <= 1) continue;
            try
            {
                var environ = File.ReadAllText(Path.Combine(procDir, "environ"));
                if (environ.Contains($"WINEPREFIX={prefix}\0", StringComparison.Ordinal))
                {
                    try { Process.GetProcessById(pid)?.Kill(); }
                    catch { }
                }
            }
            catch { }
        }
    }

    public static void ApplyWinegdkPrereqs()
    {
        Console.WriteLine("ApplyWinegdkPrereqs");
        var machine = new List<RegChange>
        {
            WineRegistry.RegDword(@"Software\Microsoft\Windows NT\CurrentVersion\OEM", "ConsoleMode", 8),
            WineRegistry.RegSz(
                @"Software\Microsoft\WindowsRuntime\ActivatableClassId\Microsoft.Windows.Storage.Pickers.FileOpenPicker",
                "DllPath", @"C:\windows\system32\windows.storage.dll"),
            WineRegistry.RegDword(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\WinHttp",
                "DefaultSecureProtocols", 2560),
            WineRegistry.RegDword(
                @"Software\Microsoft\SchannelTLS\Protocols\TLS 1.3\Client",
                "DisabledByDefault", 1),
        };

        var user = new List<RegChange>
        {
            // Wine 内置的 amd_ags_x64 无法从自身 PE 读出 AGS 版本号（err 1812），
            // 游戏（RenderDragon）会因此反复调用 agsInit 死循环：窗口白屏、主线程 100% CPU、无响应。
            // 直接禁用该 DLL，游戏会走「没有 AGS」的普通分支（本地 AMD 机器不加载它也能正常运行）。
            WineRegistry.RegSz(@"Software\Wine\DllOverrides", "amd_ags_x64", "disabled"),
            WineRegistry.RegSz("Environment", "MICROSOFT_WINDOWSAPPRUNTIME_BOOTSTRAP_INITIALIZE_SHOWUI", "0"),
            WineRegistry.RegSz("Environment", "MICROSOFT_WINDOWSAPPRUNTIME_BOOTSTRAP_INITIALIZE_FAILFAST", "0"),
            WineRegistry.RegSz("Environment", "MICROSOFT_WINDOWSAPPRUNTIME_DEPLOYMENT_INITIALIZE_ONERRORSHOWUI", "0"),
        };

        WineRegistry.UpdatePrefix(PathsList.WinePrefixPath, machine.ToArray(), user.ToArray());
        Console.WriteLine("WineGDK prereqs applied");
    }

    public static void SetRefreshToken(string token)
    {
        WineRegistry.UpdatePrefix(PathsList.WinePrefixPath,
            machine: new[] { WineRegistry.RegSz(PathsList.WinegdkReg, "RefreshToken", token) });
        Console.WriteLine("Refresh token written to Wine registry");
    }

    public static void RemoveRefreshToken()
    {
        var systemReg = Path.Combine(PathsList.WinePrefixPath, "system.reg");
        if (!File.Exists(systemReg)) return;
        WineRegistry.UpdatePrefix(PathsList.WinePrefixPath,
            machine: new[] { WineRegistry.RegDelete(PathsList.WinegdkReg, "RefreshToken") });
    }
}