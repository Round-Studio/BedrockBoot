using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogAddGameFolderContent : UserControl
{
    public string FolderPath => PathInputBox.Text;
    public string FolderName => PathNameInputBox.Text;
    public DialogAddGameFolderContent()
    {
        InitializeComponent();
    }

    private async void OpenChooseFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // ① 拿到当前 TopLevel（Window 或 Desktop 层）
        var topLevel = TopLevel.GetTopLevel(this);   // this 可以是 Window 或 UserControl
        if (topLevel == null) return;

        // ② 打开文件夹选择框
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "请选择文件夹",
                AllowMultiple = false          // true 可多选
            });

        if (folders.Count > 0)
        {
            try
            {
                string path = folders[0].Path.LocalPath; // 本地文件系统路径
                Console.WriteLine($"选择路径：{path}");

                PathInputBox.Text = path;
                PathNameInputBox.Text = Path.GetFileName(Path.GetDirectoryName(path));
            }
            catch
            {
                Console.WriteLine($"添加目录所选的路径无效");
                DialogHost.Close();
                DialogHost.Show(new DialogInfo()
                {
                    Title = "路径无效",
                    Content = "您选择的路径是无效路径\n该路径不能是磁盘根目录",
                    CloseButtonText = "好"
                });
            }
        }
    }
}