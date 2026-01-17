using System.Diagnostics;
using BedrockBoot.Models.Global;
using Microsoft.Win32;

namespace BedrockBoot.Models.Helper;

public class OpenAgreement
{
    public static void RegisterAssociation()
    {
        string appPath = Process.GetCurrentProcess().MainModule.FileName;
        string progId = "BedrockBoot.Win32";

        using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\.mcpack"))
        {
            extKey.SetValue("", progId);
        }

        using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
        {
            progIdKey.SetValue("", "Minecraft Bedrock 支持文件");
            using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
            {
                iconKey.SetValue("", $"\"{appPath}\",{SourceList.PackIconID}");
            }
            using (var cmdKey = progIdKey.CreateSubKey(@"shell\open\command"))
            {
                cmdKey.SetValue("", $"\"{appPath}\" -open \"%1\"");
            }
        }
    }
}