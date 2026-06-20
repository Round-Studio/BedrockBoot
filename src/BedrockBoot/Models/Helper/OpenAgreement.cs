using System;
using BedrockBoot.Models.Global;
using Microsoft.Win32;

namespace BedrockBoot.Models.Helper;

public class OpenAgreement
{
    public static void RegisterAssociation()
    {
        var exePath = Environment.ProcessPath;
        var ResProgId = "BedrockBoot.Win32";
        var WorldProgId = "BedrockBoot.Desktop";

        using (var extKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.mcpack"))
        {
            extKey.SetValue("", ResProgId);
        }

        using (var extKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.mcaddon"))
        {
            extKey.SetValue("", ResProgId);
        }

        using (var extKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.mcworld"))
        {
            extKey.SetValue("", WorldProgId);
        }

        using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ResProgId}"))
        {
            progIdKey.SetValue("", "Minecraft Bedrock 支持文件");
            using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
            {
                iconKey.SetValue("", $"\"{exePath}\",{SourceList.PackIconID}");
            }

            using (var cmdKey = progIdKey.CreateSubKey(@"shell\open\command"))
            {
                cmdKey.SetValue("", $"\"{exePath}\" -open --resource \"%1\"");
            }
        }

        using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{WorldProgId}"))
        {
            progIdKey.SetValue("", "Minecraft Bedrock 世界文件");
            using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
            {
                iconKey.SetValue("", $"\"{exePath}\",{SourceList.PackIconID}");
            }

            using (var cmdKey = progIdKey.CreateSubKey(@"shell\open\command"))
            {
                cmdKey.SetValue("", $"\"{exePath}\" -open --world \"%1\"");
            }
        }
    }
}