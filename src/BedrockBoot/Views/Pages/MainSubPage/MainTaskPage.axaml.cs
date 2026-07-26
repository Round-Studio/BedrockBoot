using System.Collections.Generic;
using Avalonia;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainTaskPage : BedrockBootPage
{
    /// <summary>持有回调引用以便卸载时精确解绑（改为 += 订阅，不再覆盖其他订阅者）</summary>
    private readonly System.Action _onTaskChanged;

    public MainTaskPage()
    {
        InitializeComponent();

        _onTaskChanged = () => Dispatcher.UIThread.Invoke(UpdateUI);
        GlobalModel.TaskManager.OnChanged += _onTaskChanged;
        UpdateUI();
        Unloaded += (sender, args) => UnLoad();
    }

    public void UpdateUI()
    {
        TaskList.ItemsSource = null;
        TaskViewer.IsVisible = true;
        NoneBox.IsVisible = false;

        if (GlobalModel.TaskManager.Tasks.Count <= 0)
        {
            TaskViewer.IsVisible = false;
            NoneBox.IsVisible = true;
            return;
        }

        var items = new List<Avalonia.Controls.Control>(GlobalModel.TaskManager.Tasks.Count);
        GlobalModel.TaskManager.Tasks.ForEach(task =>
        {
            task.Item.Margin = new Thickness(5);
            items.Add(task.Item);
        });
        // 一次性绑定到 ItemsSource，由 ListBox + VirtualizingStackPanel 按需实例化
        TaskList.ItemsSource = items;
    }

    public void UnLoad()
    {
        TaskList.ItemsSource = null;
        // 只解绑自己的订阅，不影响 MainWindow 等其他订阅者
        GlobalModel.TaskManager.OnChanged -= _onTaskChanged;
    }
}