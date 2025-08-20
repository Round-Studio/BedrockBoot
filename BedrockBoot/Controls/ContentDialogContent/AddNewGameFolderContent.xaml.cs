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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls.ContentDialogContent;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AddNewGameFolderContent : Page
{
    public string FolderPath => FolderPathTextBox.Text;
    public string FolderName => FolderNameTextBox.Text ?? Path.GetFileName(FolderPath);
    public AddNewGameFolderContent()
    {
        InitializeComponent();
    }

    private async void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        // 创建FolderPicker实例
        var folderPicker = new FolderPicker(WindowNative.GetWindowHandle(App.MainWindow));

        // 显示对话框并获取结果
        StorageFolder folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            FolderPathTextBox.Text = folder.Path;
            FolderNameTextBox.Text = folder.Name;
        }
    }
}
