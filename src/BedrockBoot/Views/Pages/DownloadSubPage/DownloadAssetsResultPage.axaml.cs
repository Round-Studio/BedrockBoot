using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Pages.DownloadSubPage;

public partial class DownloadAssetsResultPage : UserControl
{
    public DownloadAssetsResultPage(List<CurseForgeResponse.ModData> mods)
    {
        InitializeComponent();

        // 绑定数据源，由 ItemTemplate 按需实例化条目。
        // 旧实现逐条 Dispatcher.UIThread.Invoke 往 StackPanel 里塞控件，
        // 既无虚拟化，又会为每一条结果阻塞一次 UI 线程。
        ItemsPanel.ItemsSource = mods;
    }
}