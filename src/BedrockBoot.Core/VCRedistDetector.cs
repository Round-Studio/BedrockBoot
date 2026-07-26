using System;
using Microsoft.Win32;
using System.Collections.Generic;

namespace BedrockBoot.Core;

public class VCRedistDetector
{
    /// <summary>
    /// VC 运行库官方注册的运行时信息键，读取它可以避免枚举整个卸载列表
    /// </summary>
    private const string RuntimeKeyPath = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X64";

    private const string RuntimeKeyPathWow = @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\X64";

    /// <summary>
    /// 缓存检测结果。VC 运行库不会在启动器运行期间被安装/卸载，
    /// 因此整个进程生命周期内只需要检测一次。
    /// </summary>
    private static (bool IsInstalled, string DisplayName, string Version, string InstallDate)? _cached;

    /// <summary>
    /// 检测 Microsoft Visual C++ 2015-2022 Redistributable (x64) 是否已安装
    /// </summary>
    public static (bool IsInstalled, string DisplayName, string Version, string InstallDate) CheckInInstalledList()
    {
        if (_cached.HasValue) return _cached.Value;

        // 快路径：直接读取 VC 运行时注册键（单次键读取）
        var fast = CheckViaRuntimeKey();
        if (fast.IsInstalled)
        {
            _cached = fast;
            return fast;
        }

        // 慢路径：快路径未命中时才回退到枚举卸载列表
        var slow = CheckViaUninstallList();
        _cached = slow;
        return slow;
    }

    /// <summary>
    /// 快路径：读取 VC 运行时注册键，只需 1~2 次键打开操作
    /// </summary>
    private static (bool IsInstalled, string DisplayName, string Version, string InstallDate) CheckViaRuntimeKey()
    {
        foreach (var path in new[] { RuntimeKeyPath, RuntimeKeyPathWow })
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(path);
                if (key == null) continue;

                // Installed 为 DWORD，1 表示已安装
                if (key.GetValue("Installed") is not int installed || installed != 1) continue;

                var version = key.GetValue("Version") as string
                              ?? $"{key.GetValue("Major")}.{key.GetValue("Minor")}.{key.GetValue("Bld")}";

                return (true, "Microsoft Visual C++ 2015-2022 Redistributable (x64)", version, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"读取 VC 运行时注册键 {path} 失败: {ex.Message}");
            }
        }

        return (false, null, null, null);
    }

    /// <summary>
    /// 慢路径：枚举卸载列表（仅在快路径未命中时使用）
    /// </summary>
    private static (bool IsInstalled, string DisplayName, string Version, string InstallDate) CheckViaUninstallList()
    {
        // 要查找的程序名称模式
        string[] searchPatterns = new[]
        {
            "Microsoft Visual C++ 2015-2022 Redistributable (x64)",
            "Microsoft Visual C++ 2015-2022 (x64)",
            "Microsoft Visual C++ v14 Redistributable (x64)",
        };

        // 检查两个注册表位置
        string[] registryPaths = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",           // 64位程序
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" // 32位程序
        };

        foreach (string basePath in registryPaths)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(basePath))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey != null)
                                {
                                    // 获取显示名称
                                    string displayName = subKey.GetValue("DisplayName") as string;
                                    
                                    if (!string.IsNullOrEmpty(displayName))
                                    {
                                        // 检查是否匹配我们的搜索模式
                                        foreach (string pattern in searchPatterns)
                                        {
                                            if (displayName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                                            {
                                                // 找到匹配的程序
                                                string version = subKey.GetValue("DisplayVersion") as string;
                                                string installDate = subKey.GetValue("InstallDate") as string;
                                                string publisher = subKey.GetValue("Publisher") as string;
                                                string installLocation = subKey.GetValue("InstallLocation") as string;
                                                
                                                return (true, displayName, version, installDate);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"访问注册表路径 {basePath} 时出错: {ex.Message}");
            }
        }

        return (false, null, null, null);
    }
}