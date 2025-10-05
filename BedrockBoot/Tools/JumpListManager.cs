using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.StartScreen;

namespace BedrockBoot.Tools;

public class JumpListManager
{
    public async void CleanJumpList()
    {
        if (JumpList.IsSupported())
        {
            var list = await JumpList.LoadCurrentAsync();
            list.Items.Clear();
            await list.SaveAsync();
        }
    }
    private JumpListItem GetJumpListItem(string arguments, string displayName, string groupName)
    {
        var item = JumpListItem.CreateWithArguments(arguments, displayName);
        item.GroupName = groupName;
        item.Description = "GuenMu";
        item.Logo = new Uri("ms-appx:///Assets/guenmu.png");
        return item;
    }

    public async void SetJumpList()
    {
        if (JumpList.IsSupported())
        {
            var list = await JumpList.LoadCurrentAsync();
            list.Items.Clear();
            list.SystemGroupKind = JumpListSystemGroupKind.None;
            list.Items.Add(JumpListItem.CreateSeparator());
            var items = new List<JumpListItem> {
                GetJumpListItem("-manage", "管理已安装的版本", "常用操作"),
                GetJumpListItem("-manage", "管理已安装的版本", "常用操作"),
                GetJumpListItem("-manage", "管理已安装的版本", "不常用操作"),
            };
            foreach (var item in items)
            {
                list.Items.Add(item);
            }
            await list.SaveAsync();
        }
    }
}
