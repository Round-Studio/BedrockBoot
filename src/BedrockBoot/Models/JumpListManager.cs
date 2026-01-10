using System.Diagnostics;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;

public class JumpListManager
{
    public static void ConfigureJumpList()
    {
        if (!GlobalModel.Config.Data.IsTaskBarJumpItem)
            return;

        var jumpList = JumpList.CreateJumpList();
        jumpList.ClearAllUserTasks();

        JumpListCustomCategory myToolsCategory = new JumpListCustomCategory("快捷启动");

        try
        {
            var versions = GameInfoHelper.GetVersionConfigs(GlobalModel.Config.Data
                .GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameFolderPath);

            versions.ForEach(v =>
            {
                myToolsCategory.AddJumpListItems(
                    new JumpListLink(Process.GetCurrentProcess().MainModule.FileName, v.Info.VersionName)
                    {
                        Arguments = $"-jump \"{v.VersionPath}\"",
                        IconReference = new IconReference(Process.GetCurrentProcess().MainModule.FileName, 0),
                    });
            });
        }
        catch
        {
        }

        jumpList.AddCustomCategories(myToolsCategory);
        jumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Frequent;

        jumpList.Refresh();
    }
}