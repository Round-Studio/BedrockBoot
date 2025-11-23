using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainTaskPage : BedrockBootPage
{
    public MainTaskPage()
    {
        InitializeComponent();

        GlobalModel.TaskManager.OnChanged = () => Dispatcher.UIThread.Invoke(UpdateUI);
        UpdateUI();
        this.Unloaded += (sender, args) => UnLoad();
    }

    public void UpdateUI()
    {
        TaskViewer.IsVisible = true;
        NoneBox.IsVisible = false;
        
        if (GlobalModel.TaskManager.Tasks.Count <= 0)
        {
            TaskViewer.IsVisible = false;
            NoneBox.IsVisible = true;
        }

        GlobalModel.TaskManager.Tasks.ForEach(task =>
        {
            task.Item.Margin = new Thickness(5);
            TaskList.Children.Add(task.Item);
        });
    }

    public void UnLoad()
    {
        TaskList.Children.Clear();
        GlobalModel.TaskManager.OnChanged = null;
    }
}