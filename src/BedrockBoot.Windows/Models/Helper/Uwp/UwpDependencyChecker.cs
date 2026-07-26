using System.Diagnostics;
using System.Text;
using Windows.Management.Deployment;

namespace BedrockBoot.Models.Helper.Uwp;

public class UwpDependencyChecker
{
    private static readonly List<(string Name, string? Version)> Dependencies = new()
    {
        ("Microsoft.VCLibs.140.00", "14.0.33519.0"),
        ("Microsoft.NET.Native.Runtime.1.4", null),
        ("Microsoft.NET.Native.Runtime.2.2", "2.2.28604.0"),
        ("Microsoft.VCLibs.140.00.UWPDesktop", null),
        ("Microsoft.Services.Store.Engagement", null),
        ("Microsoft.NET.Native.Framework.1.3", null),
        ("Microsoft.NET.Native.Framework.2.2", "2.2.29512.0"),
        ("Microsoft.GamingServices", "33.108.12001.0")
    };

    /// <summary>
    /// 缓存已安装包列表。枚举一次约需数十毫秒，而 PowerShell 方案需要 1~3 秒，
    /// 且启动器运行期间包列表基本不变，因此只在首次调用时枚举。
    /// </summary>
    private static Dictionary<string, string>? _cachedPackages;

    private static readonly object CacheLock = new();

    /// <summary>
    /// 丢弃缓存，用于安装依赖之后重新检测
    /// </summary>
    public static void InvalidateCache()
    {
        lock (CacheLock) _cachedPackages = null;
    }

    public static List<(string,string)> GetMissingDependencies()
    {
        var missingDeps = new List<(string Name, string? Version)>();
        var installedPackages = GetInstalledUwpPackages();

        foreach (var dep in Dependencies)
        {
            if (!IsPackageInstalled(installedPackages, dep.Name, dep.Version))
            {
                missingDeps.Add((dep.Name, dep.Version));
            }
        }

        return missingDeps;
    }

    private static Dictionary<string, string> GetInstalledUwpPackages()
    {
        lock (CacheLock)
        {
            if (_cachedPackages != null) return _cachedPackages;
        }

        var installedPackages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 优先使用 WinRT API 直接枚举，避免启动 PowerShell 进程
        if (TryGetPackagesViaWinRt(installedPackages) == false)
        {
            // WinRT 不可用时回退到原有的 PowerShell 方案
            GetInstalledUwpPackagesViaPowerShell(installedPackages);
        }

        lock (CacheLock)
        {
            _cachedPackages = installedPackages;
        }

        return installedPackages;
    }

    /// <summary>
    /// 通过 WinRT PackageManager 枚举当前用户已安装的包
    /// </summary>
    private static bool TryGetPackagesViaWinRt(Dictionary<string, string> installedPackages)
    {
        try
        {
            var manager = new PackageManager();

            // 传入空字符串表示当前用户
            foreach (var package in manager.FindPackagesForUser(string.Empty))
            {
                var id = package.Id;
                if (id?.Name == null) continue;

                var v = id.Version;
                var version = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";

                // 同名包保留版本最高的一个
                if (installedPackages.TryGetValue(id.Name, out var existingVersion))
                {
                    if (CompareVersions(version, existingVersion) > 0)
                        installedPackages[id.Name] = version;
                }
                else
                {
                    installedPackages[id.Name] = version;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"通过 WinRT 枚举 UWP 包失败，将回退至 PowerShell: {ex.Message}");
            installedPackages.Clear();
            return false;
        }
    }

    private static void GetInstalledUwpPackagesViaPowerShell(Dictionary<string, string> installedPackages)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-AppxPackage | Select-Object Name, Version | ConvertTo-Csv -NoTypeInformation\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    Console.WriteLine(@"Failed to start PowerShell process");
                    return;
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($@"PowerShell Error: {error}");
                }

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    ParsePackageOutput(output, installedPackages);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Exception while checking installed packages: {ex.Message}");
        }
    }

    private static void ParsePackageOutput(string csvOutput, Dictionary<string, string> installedPackages)
    {
        var lines = csvOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length == 0)
            return;

        // Skip header if present
        int startIndex = 0;
        if (lines[0].Contains("Name") && lines[0].Contains("Version"))
        {
            startIndex = 1;
        }

        for (int i = startIndex; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            // Parse CSV line (handles quoted values)
            var parts = ParseCsvLine(line);
            if (parts.Count >= 2)
            {
                string name = parts[0].Trim('"');
                string version = parts[1].Trim('"');
                
                if (!string.IsNullOrEmpty(name))
                {
                    // Keep the highest version if multiple exist
                    if (installedPackages.TryGetValue(name, out var existingVersion))
                    {
                        if (CompareVersions(version, existingVersion) > 0)
                        {
                            installedPackages[name] = version;
                        }
                    }
                    else
                    {
                        installedPackages[name] = version;
                    }
                }
            }
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        result.Add(current.ToString());
        return result;
    }

    private static bool IsPackageInstalled(Dictionary<string, string> installedPackages, string packageName, string? requiredVersion)
    {
        if (!installedPackages.TryGetValue(packageName, out var installedVersion))
            return false;

        // If no specific version required, package existence is enough
        if (string.IsNullOrEmpty(requiredVersion))
            return true;

        // Check if installed version meets requirement
        return CompareVersions(installedVersion, requiredVersion) >= 0;
    }

    private static int CompareVersions(string version1, string version2)
    {
        if (string.IsNullOrEmpty(version1) || string.IsNullOrEmpty(version2))
            return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);

        if (Version.TryParse(version1, out var v1) && Version.TryParse(version2, out var v2))
        {
            return v1.CompareTo(v2);
        }
        return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
    }
}