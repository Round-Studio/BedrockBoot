using System;
using Microsoft.Win32;
using System.Collections.Generic;

namespace BedrockBoot.Core;

public class VCRedistDetector
{
    /// <summary>
    /// 检测 Microsoft Visual C++ 2015-2022 Redistributable (x64) 是否在安装列表中
    /// </summary>
    public static (bool IsInstalled, string DisplayName, string Version, string InstallDate) CheckInInstalledList()
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