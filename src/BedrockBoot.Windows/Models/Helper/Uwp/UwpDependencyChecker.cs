using System.Diagnostics;
using System.Text;

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
        var installedPackages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
                    return installedPackages;
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

        return installedPackages;
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