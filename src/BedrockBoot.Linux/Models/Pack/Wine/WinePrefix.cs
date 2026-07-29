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
        var pfx = PathsList.PreFixPath;
        if (IsReady(pfx))
        {
            Console.WriteLine("Wine prefix ready");
            return true;
        }

        Console.WriteLine("Initialising Wine prefix...");
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(ProtonCore.Config.Data.SelectProtonPath, "proton"),
            Arguments = "run wineboot -u",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.EnvironmentVariables["WINEPREFIX"] = pfx;
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
            WineRegistry.RegSz("Environment", "MICROSOFT_WINDOWSAPPRUNTIME_BOOTSTRAP_INITIALIZE_SHOWUI", "0"),
            WineRegistry.RegSz("Environment", "MICROSOFT_WINDOWSAPPRUNTIME_BOOTSTRAP_INITIALIZE_FAILFAST", "0"),
            WineRegistry.RegSz("Environment", "MICROSOFT_WINDOWSAPPRUNTIME_DEPLOYMENT_INITIALIZE_ONERRORSHOWUI", "0"),
        };

        WineRegistry.UpdatePrefix(PathsList.PreFixPath, machine.ToArray(), user.ToArray());
        Console.WriteLine("WineGDK prereqs applied");
    }

    public static void SetRefreshToken(string token)
    {
        WineRegistry.UpdatePrefix(PathsList.PreFixPath,
            machine: new[] { WineRegistry.RegSz(PathsList.PreFixPath, "RefreshToken", token) });
        Console.WriteLine("Refresh token written to Wine registry");
    }

    public static void RemoveRefreshToken()
    {
        var systemReg = Path.Combine(PathsList.PreFixPath, "system.reg");
        if (!File.Exists(systemReg)) return;
        WineRegistry.UpdatePrefix(PathsList.PreFixPath,
            machine: new[] { WineRegistry.RegDelete(PathsList.WinegdkReg, "RefreshToken") });
    }
}