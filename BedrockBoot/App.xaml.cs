using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using BedrockBoot.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public static Window? _window;
        public static Window MainWindow = Window.Current;
        public static MainWindow MWindow;
        public JsonNavigationService NavService { get; set; }
        public IThemeService AppThemeService { get; set; }

        private void TraverseAllControls(UIElement p)
        {
            // 从当前窗口的根容器开始遍历
            var allControls = GetAllControls(p);

            // 现在你可以遍历所有控件了
            foreach (var control in allControls)
            {
                DragMoveAndResizeHelper.SetDragMove(App._window, control);
            }
        }

        // 递归获取所有控件
        private List<UIElement> GetAllControls(UIElement parent)
        {
            var controls = new List<UIElement>();

            if (parent == null)
                return controls;

            // 添加当前控件
            controls.Add(parent);

            // 如果当前控件是容器，递归获取其子控件
            if (parent is Panel panel)
            {
                foreach (UIElement child in panel.Children)
                {
                    controls.AddRange(GetAllControls(child));
                }
            }
            else if (parent is ContentControl contentControl && contentControl.Content is UIElement contentElement)
            {
                controls.AddRange(GetAllControls(contentElement));
            }
            else if (parent is Border border && border.Child is UIElement borderChild)
            {
                controls.AddRange(GetAllControls(borderChild));
            }
            else if (parent is ItemsControl itemsControl)
            {
                // 处理 ItemsControl（如 ListView、ListBox 等）
                foreach (var item in itemsControl.Items)
                {
                    if (itemsControl.ItemContainerGenerator.ContainerFromItem(item) is UIElement container)
                    {
                        controls.AddRange(GetAllControls(container));
                    }
                }
            }
            // 可以添加更多容器类型的处理...

            return controls;
        }

        public App()
        {
            global_cfg.InitConfig();
            global_cfg.core.Downloader = new ImprovedFlexibleMultiThreadDownloader(global_cfg.cfg.JsonCfg.DownThread);
            InitializeComponent();

            NavService = new JsonNavigationService(); // JsonNav特有的删不干净
            MainWindow mainWindow = new MainWindow();
            MainWindow = mainWindow;
            MWindow = mainWindow;
            MainWindow.Title = MainWindow.AppWindow.Title = ProcessInfoHelper.ProductNameAndVersion;
            AppThemeService = new ThemeService(_window);
            AppThemeService.AutoInitialize(_window);
            AppThemeService.AutoUpdateTitleBarCaptionButtonsColor();
            // AppThemeService.ConfigureBackdrop(BackdropType.AcrylicThin);
        }
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
            TraverseAllControls(_window.Content);
        }
    }
}
