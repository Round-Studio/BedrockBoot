using BedrockBoot.Controls.ContentDialogContent;
using BedrockBoot.Models.Classes.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ToolsBoxPage : Page
    {
        public ToolsBoxPage()
        {
            InitializeComponent();
            Unloaded += Page_Unloaded;

            MouseLockToggleSwitch.IsOn = global_cfg.cfg.JsonCfg.MouseLock;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            global_cfg.cfg.JsonCfg.MouseLock = MouseLockToggleSwitch.IsOn;


            global_cfg.cfg.SaveConfig();
            if (!global_cfg.cfg.JsonCfg.MouseLock)
            {
                MouseHelper.StopMouseLock();
            }
            else
            {
                MouseHelper.StartMouseLock();
            }
        }
        private async void MouseLock_Setting(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Content = new SettingMouseLockPaddingContent(),
                Title = "鼠标锁设置",
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };
            var res = await dialog.ShowAsync();
            if(res == ContentDialogResult.Primary)
            {
                global_cfg.cfg.JsonCfg.MouseLockCutPX = ((SettingMouseLockPaddingContent)dialog.Content).MouseLockPadding;
            }
        }
    }
}
