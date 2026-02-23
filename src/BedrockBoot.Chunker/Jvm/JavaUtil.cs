// 本段部分实现逻辑参考自 ProjBobcat 的 DeepJavaSearcher.cs
// 仓库地址：https://github.com/Corona-Studio/ProjBobcat 

using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using BedrockBoot.Chunker.Base.Entry.Info;

namespace BedrockBoot.Chunker.Jvm;

public static partial class JavaUtil {
    public static async Task<JavaInfo> GetJavaInfoAsync(string javaPath, CancellationToken cancellationToken = default) {
        if (string.IsNullOrEmpty(javaPath) || !File.Exists(javaPath)) {
            return null;
        }

        using var process = Process.Start(new ProcessStartInfo(javaPath) {
            Arguments = "-version",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        });

        string text = process.StandardError.ReadToEnd();
        if (string.IsNullOrEmpty(text))
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

        bool is64bit = text.Contains("64-bit", StringComparison.OrdinalIgnoreCase);
        string javaVersion = JavaVersionRegex().Match(text).Groups["version"].Value;

        string javaType = text.Contains("java(tm)", StringComparison.OrdinalIgnoreCase)
            ? "Java"
            : text.Contains("zulu")
                ? "ZuluJDK"
                : "OpenJDK";

        await process.WaitForExitAsync(cancellationToken);

        var versionParts = javaVersion.Split(".");
        return new JavaInfo {
            Is64bit = is64bit,
            JavaPath = javaPath,
            JavaType = javaType,
            JavaVersion = javaVersion,
            MajorVersion = (int.Parse(versionParts[0]) == 1) ? int.Parse(versionParts[1]) : int.Parse(versionParts[0]),
        };
    }

    public static async Task<List<JavaInfo>> GetJavaListAsync(CancellationToken cancellationToken = default) {
        var javaList = new List<JavaInfo>();
        
        if (OperatingSystem.IsWindows()) {
            foreach (var java in GetJavasForWindows()) {
                if (File.Exists(java)) {
                    var javaInfo = await GetJavaInfoAsync(java, cancellationToken);
                    if (javaInfo != null) {
                        javaList.Add(javaInfo);
                    }
                }
            }
            return javaList;
        }

        using var process = Process.Start(new ProcessStartInfo("whereis") {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            ArgumentList = {
                "/b",
                "java"
            },
        });

        if (process == null)
            return javaList;

        do {
            cancellationToken.ThrowIfCancellationRequested();

            var line = process.StandardOutput.ReadLine();
            if (string.IsNullOrEmpty(line) || !File.Exists(line))
                continue;

            var javaInfo = await GetJavaInfoAsync(line, cancellationToken);
            if (javaInfo != null) {
                javaList.Add(javaInfo);
            }
        } while (!process.HasExited);

        await process.WaitForExitAsync(cancellationToken);
        var lastLine = await process.StandardOutput.ReadLineAsync(cancellationToken);

        if (!string.IsNullOrEmpty(lastLine) && File.Exists(lastLine)) {
            var javaInfo = await GetJavaInfoAsync(lastLine, cancellationToken);
            if (javaInfo != null) {
                javaList.Add(javaInfo);
            }
        }

        return javaList;
    }

    #region Privates
    private static Regex JavaVersionRegex() => new Regex("(java|openjdk) version \"\\s*(?<version>\\S+)\\s*\"");

    [SupportedOSPlatform("Windows")]
    private static IEnumerable<string> GetJavasForWindows() {
        //Use by:https://github.com/Xcube-Studio/Natsurainko.FluentCore/blob/main/Natsurainko.FluentCore/Environment/JavaUtils.cs 
        List<string> result = [];

        #region Cmd: Find Java by running "where java" command in cmd.exe

        using var process = new Process() {
            StartInfo = new ProcessStartInfo() {
                FileName = "cmd",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true,
        };

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        var output = new List<string>();

        process.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
                output.Add(e.Data);
        };
        process.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
                output.Add(e.Data);
        };

        process.StandardInput.WriteLine("where java");
        process.StandardInput.WriteLine("exit");
        process.WaitForExit();

        IEnumerable<string> javaPaths = output.Where(
            x => !string.IsNullOrEmpty(x) && x.EndsWith("java.exe") && File.Exists(x)
        )!; // null checked in the where clause
        result.AddRange(javaPaths);

        #endregion

        #region Registry: Find Java by searching the registry

        var javaHomePaths = new List<string>();

        // Local function: recursively search for the keyName in the registry
        static List<string> ForRegistryKey(RegistryKey registryKey, string keyName) {
            var result = new List<string>();

            foreach (string valueName in registryKey.GetValueNames()) {
                if (valueName == keyName) // Check that the valueName exists
                    result.Add((string)registryKey.GetValue(valueName)!);
            }

            foreach (string registrySubKey in registryKey.GetSubKeyNames()) {
                using var subKey = registryKey.OpenSubKey(registrySubKey);
                if (subKey is not null) // Check that the registrySubKey exists
                    result.AddRange(ForRegistryKey(subKey, keyName));
            }

            return result;
        }
        ;

        using var reg = Registry.LocalMachine.OpenSubKey("SOFTWARE");

        if (reg is not null && reg.GetSubKeyNames().Contains("JavaSoft")) {
            using var registryKey = reg.OpenSubKey("JavaSoft");
            if (registryKey is not null)
                javaHomePaths.AddRange(ForRegistryKey(registryKey, "JavaHome"));
        }

        if (reg is not null && reg.GetSubKeyNames().Contains("WOW6432Node")) {
            using var registryKey = reg.OpenSubKey("WOW6432Node");
            if (registryKey is not null && registryKey.GetSubKeyNames().Contains("JavaSoft")) {
                using var registrySubKey = reg.OpenSubKey("JavaSoft");
                if (registrySubKey is not null)
                    ForRegistryKey(registrySubKey, "JavaHome").ForEach(x => javaHomePaths.Add(x));
            }
        }

        foreach (var item in javaHomePaths)
            if (Directory.Exists(item))
                result.AddRange(GetFilesInDirectory(item, "java.exe"));

        #endregion

        #region Special Folders

        List<string> folders = [];

        // %APPDATA%\.minecraft\cache\java
        string appDataPath = Environment.GetEnvironmentVariable("APPDATA");

        if (!string.IsNullOrEmpty(appDataPath))
            folders.Add(Path.Combine(appDataPath, ".minecraft\\cache\\java"));

        // %APPDATA%\.minecraft\runtime\
        if (!string.IsNullOrEmpty(appDataPath))
            folders.Add(Path.Combine(appDataPath, ".minecraft\\runtime\\"));

        // %JAVA_HOME%
        string javaHomePath = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (javaHomePath is not null)
            folders.Add(javaHomePath);

        // Program Files\Java
        folders.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Java"));
        folders.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Java"));

        // Program Files\Zulu
        folders.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zulu"));
        folders.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Zulu"));

        // Check Java for each folder
        foreach (var folder in folders)
            if (Directory.Exists(folder))
                result.AddRange(GetFilesInDirectory(folder, "java.exe"));

        #endregion

        return result.Distinct();
    }

    private static IEnumerable<string> GetFilesInDirectory(string directoryPath, string searchPattern) {
        var files = new List<string>();
        
        try {
            // 使用 Directory.GetFiles 递归搜索文件
            files.AddRange(Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories));
        }
        catch (UnauthorizedAccessException) {
            // 忽略无权限访问的目录
        }
        catch (Exception) {
            // 忽略其他异常（如路径过长等）
        }
        
        return files;
    }

    #endregion
}