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

// 本段部分实现逻辑参考自 ProjBobcat 的 DeepJavaSearcher.cs
// 仓库地址：https://github.com/Corona-Studio/ProjBobcat 

using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using BedrockBoot.Chunker.Base.Entry.Info;

namespace BedrockBoot.Chunker.Jvm;

public static partial class JavaUtil
{
    /// <summary>
    ///     探测指定 java.exe 的版本信息。
    ///     任何探测失败（文件损坏、代理壳、无输出、版本号无法解析）都返回 null，交由调用方跳过，
    ///     绝不能因为单个无效的 java.exe 中断整个扫描流程。
    /// </summary>
    public static async Task<JavaInfo?> GetJavaInfoAsync(string javaPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(javaPath) || !File.Exists(javaPath))
        {
            return null;
        }

        Process? process;

        try
        {
            process = Process.Start(new ProcessStartInfo(javaPath)
            {
                Arguments = "-version",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            });
        }
        catch (Exception)
        {
            // Oracle javapath 之类的转发壳、被卸载后残留的 java.exe、权限不足等
            // 都会在这里抛 Win32Exception，直接视为无效候选
            return null;
        }

        if (process is null)
        {
            return null;
        }

        using (process)
        {
            // 两个流必须同时消费，否则缓冲区写满会导致 java 进程阻塞、WaitForExitAsync 永远等不到
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            // 规范实现把 -version 写到 stderr，但部分发行版/包装脚本会写 stdout，两者都取
            string text =
                await errorTask.ConfigureAwait(false) + await outputTask.ConfigureAwait(false);

            // 残缺的 java.exe 可能启动后一个字符都不输出，此时它是无效候选而非异常
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var versionMatch = JavaVersionRegex().Match(text);
            if (!versionMatch.Success)
            {
                return null;
            }

            string javaVersion = versionMatch.Groups["version"].Value;
            string[] versionParts = javaVersion.Split('.');

            // 1.8.0_392 -> 8；17.0.1 / 21.0.1+12 -> 主版本号
            int majorVersion;
            if (versionParts[0] == "1")
            {
                if (versionParts.Length < 2 || !int.TryParse(versionParts[1], out majorVersion))
                    return null;
            }
            else if (!int.TryParse(versionParts[0], out majorVersion))
            {
                return null;
            }

            bool is64bit = text.Contains("64-bit", StringComparison.OrdinalIgnoreCase);

            string javaType = text.Contains("java(tm)", StringComparison.OrdinalIgnoreCase)
                ? "Java"
                : text.Contains("zulu")
                    ? "ZuluJDK"
                    : "OpenJDK";

            return new JavaInfo
            {
                Is64bit = is64bit,
                JavaPath = javaPath,
                JavaType = javaType,
                JavaVersion = javaVersion,
                MajorVersion = majorVersion
            };
        }
    }

    public static async Task<List<JavaInfo>> GetJavaListAsync(CancellationToken cancellationToken = default)
    {
        var javaList = new List<JavaInfo>();

        if (OperatingSystem.IsWindows())
        {
            foreach (var java in GetJavasForWindows())
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // 单个候选异常不应影响其它候选的探测结果
                    var javaInfo = await GetJavaInfoAsync(java, cancellationToken);
                    if (javaInfo != null)
                    {
                        javaList.Add(javaInfo);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"Skip invalid java '{java}': {ex.Message}");
                }
            }

            return javaList;
        }

        using var process = Process.Start(new ProcessStartInfo("whereis")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            ArgumentList =
            {
                "/b",
                "java"
            },
        });

        if (process == null)
            return javaList;

        // 直接把 stdout 读完再按行切分，避免用 HasExited 轮询造成的丢行/空转
        string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(line))
                continue;

            try
            {
                var javaInfo = await GetJavaInfoAsync(line, cancellationToken);
                if (javaInfo != null)
                {
                    javaList.Add(javaInfo);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Skip invalid java '{line}': {ex.Message}");
            }
        }

        return javaList;
    }

    #region Privates

    private static Regex JavaVersionRegex() => new Regex("(java|openjdk) version \"\\s*(?<version>\\S+)\\s*\"");

    [SupportedOSPlatform("Windows")]
    private static IEnumerable<string> GetJavasForWindows()
    {
        //Use by:https://github.com/Xcube-Studio/Natsurainko.FluentCore/blob/main/Natsurainko.FluentCore/Environment/JavaUtils.cs 
        List<string> result = [];

        #region Cmd: Find Java by running "where java" command in cmd.exe

        using var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true,
        };

        var output = new List<string>();

        // 事件必须在 Start / BeginOutputReadLine 之前挂上，否则最早输出的几行会丢失，
        // 极端情况下 "where java" 的全部结果都收不到
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                lock (output)
                {
                    output.Add(e.Data);
                }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                lock (output)
                {
                    output.Add(e.Data);
                }
        };

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        process.StandardInput.WriteLine("where java");
        process.StandardInput.WriteLine("exit");
        process.WaitForExit();

        IEnumerable<string> javaPaths;
        lock (output)
        {
            javaPaths = output
                .Where(x => !string.IsNullOrWhiteSpace(x) && x.EndsWith("java.exe") && File.Exists(x))
                .ToList();
        }

        result.AddRange(javaPaths);

        #endregion

        #region Registry: Find Java by searching the registry

        var javaHomePaths = new List<string>();

        // Local function: recursively search for the keyName in the registry
        static List<string> ForRegistryKey(RegistryKey registryKey, string keyName)
        {
            var result = new List<string>();

            foreach (string valueName in registryKey.GetValueNames())
            {
                if (valueName == keyName) // Check that the valueName exists
                    result.Add((string)registryKey.GetValue(valueName)!);
            }

            foreach (string registrySubKey in registryKey.GetSubKeyNames())
            {
                using var subKey = registryKey.OpenSubKey(registrySubKey);
                if (subKey is not null) // Check that the registrySubKey exists
                    result.AddRange(ForRegistryKey(subKey, keyName));
            }

            return result;
        }

        ;

        using var reg = Registry.LocalMachine.OpenSubKey("SOFTWARE");

        if (reg is not null && reg.GetSubKeyNames().Contains("JavaSoft"))
        {
            using var registryKey = reg.OpenSubKey("JavaSoft");
            if (registryKey is not null)
                javaHomePaths.AddRange(ForRegistryKey(registryKey, "JavaHome"));
        }

        if (reg is not null && reg.GetSubKeyNames().Contains("WOW6432Node"))
        {
            using var registryKey = reg.OpenSubKey("WOW6432Node");
            if (registryKey is not null && registryKey.GetSubKeyNames().Contains("JavaSoft"))
            {
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

    private static IEnumerable<string> GetFilesInDirectory(string directoryPath, string searchPattern)
    {
        var files = new List<string>();

        try
        {
            // 使用 Directory.GetFiles 递归搜索文件
            files.AddRange(Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories));
        }
        catch (UnauthorizedAccessException)
        {
            // 忽略无权限访问的目录
        }
        catch (Exception)
        {
            // 忽略其他异常（如路径过长等）
        }

        return files;
    }

    #endregion
}