using System.Diagnostics;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;

public class JumpListManager
{
    public static void ConfigureJumpList()
    {
        var jumpList = JumpList.CreateJumpList();
        if (!BedrockBoot.Core.Global.GlobalModel.Config.Data.IsTaskBarJumpItem)
        {
            jumpList.ClearAllUserTasks();
            jumpList.Refresh();
            return;
        }
        try
        {
            var versions = GameInfoHelper.GetVersionConfigs(BedrockBoot.Core.Global.GlobalModel.Config.Data
                .GameFolders[BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolderSelIndex].GameFolderPath);

           var tasks = versions.Select(v =>
               new JumpListLink(Process.GetCurrentProcess().MainModule!.FileName, v.Info.VersionName)
               {
                   Arguments = $"-jump \"{v.VersionPath}\"",
                    IconReference = new IconReference(Process.GetCurrentProcess().MainModule!.FileName,
                        SourceList.MinecraftIconID)
               });

           Console.WriteLine($@"添加跳转列表快捷启动项");

 		   jumpList.AddUserTasks(tasks.ToArray());
           jumpList.Refresh();
		}
        catch
        {
        }
    }
}