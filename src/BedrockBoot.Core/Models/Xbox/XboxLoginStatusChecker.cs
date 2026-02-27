using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Info.Xbox;
using Microsoft.Win32;

namespace BedrockBoot.Core.Models.Xbox;

public class XboxLoginStatusChecker
{
    // 添加 Windows API 调用
    [DllImport("xboxservices.dll", CharSet = CharSet.Unicode)]
    private static extern int XblGetUserInfo(uint index, out IntPtr gamertag, out IntPtr xuid);
    
    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
    
    public async Task<XboxStatus> GetDetailedXboxStatus()
    {
        var status = new XboxStatus();
        
        Console.WriteLine(@"正在检测 Xbox 登录状态...");
        
        // 方法1：检查服务
        status.XblAuthServiceRunning = IsServiceRunning("XblAuthManager");
        status.XboxNetApiServiceRunning = IsServiceRunning("XboxNetApiSvc");
        status.XboxGipSvcRunning = IsServiceRunning("XboxGipSvc");
        
        // 方法2：检查注册表 - 尝试多个可能的路径
        status.RegistryUserFound = CheckRegistryForUser();
        
        // 方法3：检查进程 - 扩展进程列表
        status.XboxProcessesRunning = CheckXboxProcesses();
        
        // 方法4：尝试通过 Xbox API 获取用户信息
        status.XboxApiUserFound = CheckXboxApiForUser();
        
        // 方法5：检查 Xbox 应用包状态
        status.XboxAppInstalled = CheckXboxAppPackages();
        
        // 方法6：检查 Windows 令牌缓存
        status.WindowsTokenFound = CheckWindowsTokenCache();
        
        // 方法7：获取详细的用户信息（如果可能）
        status.XboxUserInfo = GetXboxUserInfo();
        
        // 综合判断（更准确的逻辑）
        status.IsLoggedIn = DetermineLoginStatus(status);
        
        return status;
    }
    
    private bool IsServiceRunning(string serviceName)
    {
        try
        {
            using (var sc = new ServiceController(serviceName))
            {
                return sc.Status == ServiceControllerStatus.Running;
            }
        }
        catch (InvalidOperationException)
        {
            // 服务不存在
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    private bool CheckRegistryForUser()
    {
        try
        {
            // 尝试多个可能的注册表路径
            string[] registryPaths = new[]
            {
                @"Software\Microsoft\XboxLive",
                @"Software\Microsoft\XboxLive\Identity",
                @"Software\Microsoft\XboxLive\Authentication",
                @"Software\Microsoft\Xbox\Identity",
                @"Software\Microsoft\Xbox\XboxLive",
                @"Software\Microsoft\XboxLive\User"
            };
            
            foreach (var path in registryPaths)
            {
                using (var key = Registry.CurrentUser.OpenSubKey(path))
                {
                    if (key != null)
                    {
                        // 检查各种可能的用户标识字段
                        var xuid = key.GetValue("UserXUID")?.ToString();
                        var gamertag = key.GetValue("Gamertag")?.ToString();
                        var userId = key.GetValue("UserId")?.ToString();
                        var xboxUserId = key.GetValue("XboxUserId")?.ToString();
                        var displayName = key.GetValue("DisplayName")?.ToString();
                        
                        // 打印找到的信息（调试用）
                        if (!string.IsNullOrEmpty(gamertag))
                        {
                            Console.WriteLine($@"找到 Gamertag: {gamertag}");
                        }
                        
                        if (!string.IsNullOrEmpty(xuid) && xuid != "0" ||
                            !string.IsNullOrEmpty(gamertag) ||
                            !string.IsNullOrEmpty(userId) ||
                            !string.IsNullOrEmpty(xboxUserId) ||
                            !string.IsNullOrEmpty(displayName))
                        {
                            return true;
                        }
                    }
                }
            }
            
            // 检查 IdentityCRL 中的 Xbox 相关令牌
            using (var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\IdentityCRL\TokenCache"))
            {
                if (key != null)
                {
                    var subKeyNames = key.GetSubKeyNames();
                    if (subKeyNames.Any(name => name.Contains("xbox") || 
                                                name.Contains("live.com") ||
                                                name.Contains("xbl")))
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"注册表检查出错: {ex.Message}");
        }
        return false;
    }
    
    private bool CheckXboxProcesses()
    {
        // 扩展进程列表
        var processes = new[] 
        { 
            "XboxAppServices",      // Xbox 应用服务
            "XboxGipSvc",           // Xbox 配件服务
            "GameBar",              // Windows 游戏栏
            "GameBarFTServer",      // 游戏栏服务
            "GameBarPresenceWriter",// 游戏栏状态
            "XboxPcApp",            // Xbox PC 应用
            "XboxGameCallableUI",   
            "XboxNetApiSvc",        // Xbox 网络 API 服务
            "XblAuthManager",       // 认证服务进程
            "XboxIdentityProvider", // Xbox 身份提供者
            "XboxStat",             // Xbox 统计
            "XboxSocial",           // Xbox 社交
            "Microsoft.Xbox.Gaming.Shell" // Xbox 游戏 Shell
        };
        
        var runningProcesses = processes.Where(p => 
        {
            try
            {
                return Process.GetProcessesByName(p).Length > 0;
            }
            catch
            {
                return false;
            }
        }).ToList();
        
        if (runningProcesses.Any())
        {
            Console.WriteLine($@"运行的 Xbox 进程: {string.Join(", ", runningProcesses)}");
            return true;
        }
        
        return false;
    }
    
    private bool CheckXboxApiForUser()
    {
        try
        {
            IntPtr gamertagPtr;
            IntPtr xuidPtr;
            
            int result = XblGetUserInfo(0, out gamertagPtr, out xuidPtr);
            
            if (result == 0 && gamertagPtr != IntPtr.Zero)
            {
                string gamertag = Marshal.PtrToStringUni(gamertagPtr);
                
                if (xuidPtr != IntPtr.Zero)
                {
                    string xuid = Marshal.PtrToStringUni(xuidPtr);
                    Console.WriteLine($@"Xbox API - Gamertag: {gamertag}, XUID: {xuid}");
                }
                
                // 释放内存
                LocalFree(gamertagPtr);
                LocalFree(xuidPtr);
                
                return !string.IsNullOrEmpty(gamertag);
            }
        }
        catch (DllNotFoundException)
        {
            // xboxservices.dll 可能不存在，静默忽略
        }
        catch (EntryPointNotFoundException)
        {
            // API 入口点可能不存在
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Xbox API 检查出错: {ex.Message}");
        }
        
        return false;
    }
    
    private bool CheckXboxAppPackages()
    {
        try
        {
            // 使用 PowerShell 命令检查 Xbox 应用包状态
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-Command \"Get-AppxPackage -Name 'Microsoft.Xbox*' | Select-Object Name, PackageFullName, Status | ConvertTo-Json -Compress\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            
            using var process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            
            if (!process.WaitForExit(5000))
            {
                process.Kill();
                return false;
            }
            
            if (!string.IsNullOrWhiteSpace(output) && 
                (output.Contains("Microsoft.Xbox") || output.Contains("Xbox")))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Xbox 应用包检查出错: {ex.Message}");
        }
        
        return false;
    }
    
    private bool CheckWindowsTokenCache()
    {
        try
        {
            // 检查 Windows 账户中是否有 Xbox 身份
            using (var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\IdentityCRL\TokenCache"))
            {
                if (key != null)
                {
                    var subKeyNames = key.GetSubKeyNames();
                    // 检查是否有 Xbox 相关的令牌
                    var xboxTokens = subKeyNames.Where(name => 
                        name.Contains("xbox") || 
                        name.Contains("live.com") || 
                        name.Contains("xbl") ||
                        name.Contains("xsts")).ToList();
                    
                    if (xboxTokens.Any())
                    {
                        Console.WriteLine($@"找到 Xbox 令牌: {string.Join(", ", xboxTokens)}");
                        return true;
                    }
                }
            }
            
            // 检查 MSAL 缓存
            using (var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\MSAL\Cache"))
            {
                if (key != null)
                {
                    var subKeyNames = key.GetSubKeyNames();
                    if (subKeyNames.Any(name => name.Contains("xbox")))
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"令牌缓存检查出错: {ex.Message}");
        }
        
        return false;
    }
    
    private XboxUserInfo GetXboxUserInfo()
    {
        var userInfo = new XboxUserInfo();
        
        try
        {
            // 尝试从注册表获取更多用户信息
            string[] registryPaths = new[]
            {
                @"Software\Microsoft\XboxLive",
                @"Software\Microsoft\Xbox\Identity"
            };
            
            foreach (var path in registryPaths)
            {
                using (var key = Registry.CurrentUser.OpenSubKey(path))
                {
                    if (key != null)
                    {
                        userInfo.XUID = key.GetValue("UserXUID")?.ToString() ?? 
                                       key.GetValue("Xuid")?.ToString();
                        userInfo.Gamertag = key.GetValue("Gamertag")?.ToString();
                        userInfo.UserId = key.GetValue("UserId")?.ToString();
                        userInfo.Email = key.GetValue("Email")?.ToString();
                        userInfo.DisplayName = key.GetValue("DisplayName")?.ToString();
                        
                        // 获取令牌过期时间
                        var expirationObj = key.GetValue("TicketExpiration");
                        if (expirationObj != null && expirationObj is byte[] bytes && bytes.Length >= 8)
                        {
                            long fileTime = BitConverter.ToInt64(bytes, 0);
                            userInfo.TokenExpiration = DateTime.FromFileTime(fileTime);
                        }
                        
                        break;
                    }
                }
            }
        }
        catch { }
        
        return userInfo;
    }
    
    private bool DetermineLoginStatus(XboxStatus status)
    {
        // 综合判断逻辑
        int score = 0;
        
        // 核心指标 - 高权重
        if (status.XblAuthServiceRunning) score += 30;
        if (status.XboxApiUserFound) score += 30;
        if (status.RegistryUserFound) score += 25;
        
        // 辅助指标 - 中等权重
        if (status.WindowsTokenFound) score += 20;
        if (!string.IsNullOrEmpty(status.XboxUserInfo?.Gamertag)) score += 20;
        if (status.XboxNetApiServiceRunning) score += 15;
        
        // 低权重指标
        if (status.XboxProcessesRunning) score += 10;
        if (status.XboxAppInstalled) score += 5;
        if (status.XboxGipSvcRunning) score += 5;
        
        // 打印得分
        Console.WriteLine($@"登录状态得分: {score}/100");
        
        // 得分阈值判断
        return score >= 30; // 30分以上视为已登录
    }
}