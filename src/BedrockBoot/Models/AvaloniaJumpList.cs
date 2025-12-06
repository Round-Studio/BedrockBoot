using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;

public class JumpListManager
{
    public void ConfigureJumpList()
    {
        var jumpList = JumpList.CreateJumpList();
        jumpList.ClearAllUserTasks();

        JumpListCustomCategory myToolsCategory = new JumpListCustomCategory("快捷启动");

        myToolsCategory.AddJumpListItems(new JumpListLink(Process.GetCurrentProcess().MainModule.FileName, "1.21.100")
        {
            Arguments = "/new",
            IconReference = new IconReference(Process.GetCurrentProcess().MainModule.FileName, 0),
        });

        jumpList.AddCustomCategories(myToolsCategory);
        jumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Frequent;

        jumpList.Refresh();
    }
}