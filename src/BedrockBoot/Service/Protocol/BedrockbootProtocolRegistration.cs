using System;
using Microsoft.Win32;

namespace BedrockBoot.Service.Protocol;

public static class BedrockbootProtocolRegistration
{
    private const string ProtocolName = "bedrockboot";
    private const string ProtocolDescription = "BedrockBoot - Minecraft 基岩版启动器";

    public static void Register()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            Console.WriteLine(@"无法获取可执行文件路径，协议注册失败");
            return;
        }

        var protocolPath = $@"Software\Classes\{ProtocolName}";

        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey(protocolPath))
            {
                key.SetValue("", ProtocolDescription);
                key.SetValue("URL Protocol", "");

                using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                {
                    defaultIcon.SetValue("", $"\"{exePath}\",0");
                }

                using (var shell = key.CreateSubKey(@"shell\open\command"))
                {
                    shell.SetValue("", $"\"{exePath}\" -bedrockboot \"%1\"");
                }
            }

            RegisterForWindows11(protocolPath, exePath);

            Console.WriteLine($@"协议 {ProtocolName}:// 注册成功！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"协议 {ProtocolName}:// 注册失败: {ex.Message}");
        }
    }

    private static void RegisterForWindows11(string protocolPath, string applicationPath)
    {
        var appName = System.IO.Path.GetFileNameWithoutExtension(applicationPath);
        var capabilitiesPath = $@"Software\{appName}\Capabilities";

        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey(capabilitiesPath))
            {
                key.SetValue("ApplicationDescription", ProtocolDescription);
                key.SetValue("ApplicationName", appName);

                using (var urlAssociations = key.CreateSubKey("URLAssociations"))
                {
                    urlAssociations.SetValue(ProtocolName, ProtocolDescription);
                }
            }

            using (var registeredApps = Registry.CurrentUser.CreateSubKey(
                       @"Software\RegisteredApplications"))
            {
                registeredApps.SetValue(ProtocolDescription, capabilitiesPath);
            }
        }
        catch
        {
        }
    }

    public static void Unregister()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProtocolName}", false);
            Console.WriteLine($@"协议 {ProtocolName}:// 已卸载");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"卸载协议失败: {ex.Message}");
        }
    }
}
