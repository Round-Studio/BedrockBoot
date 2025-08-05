
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using BedrockBoot.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// DM: 这是个什么东西？
    /// </summary>
   public sealed partial class TaskPage : Page
    {
        public ObservableCollection<TaskCard> TaskList { get; set; } = global_cfg.tasksPool;
        public TaskPage()
        {
            InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            BreadcrumbBar.ItemsSource = new string[] { "任务列表" };
            Loaded += TaskPage_Loaded;
            Unloaded += TaskPage_Unloaded;
            global_cfg.tasksPool.CollectionChanged += TasksPool_CollectionChanged;
        }

        private void TaskPage_Unloaded(object sender, RoutedEventArgs e)
        {
            foreach (var taskCard in global_cfg.tasksPool)
            {
                TaskContainer.Children.Remove(taskCard);
            }
        }

        private void TasksPool_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var eOldItem in e.OldItems)
                {
                    TaskContainer.Children.Remove((TaskCard)eOldItem);
                }

                GC.Collect(GC.MaxGeneration);
            }
        }
        
        private void TaskPage_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var card in TaskList)
            {
                card.HorizontalAlignment = HorizontalAlignment.Stretch;
                card.Width = this.Width;
                TaskContainer.Children.Add(card);
            }
        }
    }
}
