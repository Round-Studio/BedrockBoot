using System;
using System.Diagnostics;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
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

        var myToolsCategory = new JumpListCustomCategory("快捷启动");

        try
        {
            var versions = GameInfoHelper.GetVersionConfigs(BedrockBoot.Core.Global.GlobalModel.Config.Data
                .GameFolders[BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolderSelIndex].GameFolderPath);

            versions.ForEach(v =>
            {
                myToolsCategory.AddJumpListItems(
                    new JumpListLink(Process.GetCurrentProcess().MainModule.FileName, v.Info.VersionName)
                    {
                        Arguments = $"-jump \"{v.VersionPath}\"",
                        IconReference = new IconReference(Process.GetCurrentProcess().MainModule.FileName,
                            SourceList.MinecraftIconID)
                    });

                Console.WriteLine($@"添加任务栏快捷启动项 {v.Info.VersionName}");
            });

            jumpList.AddCustomCategories(myToolsCategory);
            jumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Frequent;

            jumpList.Refresh();
        }
        catch
        {
        }
    }
}