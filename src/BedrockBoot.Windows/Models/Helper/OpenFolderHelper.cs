using System.Diagnostics;
using BedrockBoot.Base.Entry;

namespace BedrockBoot.Models.Helper;

public class OpenFolderHelper
{
    public static void Open(string folder)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true // 使用外壳程序打开文件夹
        });
    }
}