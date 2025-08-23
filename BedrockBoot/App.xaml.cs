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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Maps;

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
            AppThemeService = new ThemeService(MainWindow);
            AppThemeService.AutoUpdateTitleBarCaptionButtonsColor();
        }
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
