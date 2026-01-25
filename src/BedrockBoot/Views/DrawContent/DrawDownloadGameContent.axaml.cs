using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Models.Global;
using BedrockBoot.Services;
using BedrockBoot.Views.Control;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawDownloadGameContent : UserControl
{
    public BuildInfo BuildInfo { get; set; }
    public List<GameDownloadUrlInfo>? Sources;
    public DrawDownloadGameContent()
    {
        InitializeComponent();
    }

    public DrawDownloadGameContent(BuildInfo info) : this()
    {
        BuildInfo = info;

        UpdateUI();
    }

    public void UpdateUI()
    {
        GlobalModel.Config.Data.GameFolders.ForEach(folder =>
            InstallFolder.Items.Add($"[{folder.GameFolderName}] {folder.GameFolderPath}"));

        InstallFolder.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;
        InstallName.Text = BuildInfo.ID;

        Task.Run(() =>
        {
            Sources = EasyDownload.GetPackageUrls(BuildInfo).Result;
            if (Sources != null)
            {
                Sources.ForEach(urlInfo =>
                {
                    Dispatcher.UIThread.Invoke(() => SourceSelBox.Items.Add(new ListBoxItem()
                    {
                        Content = new GameDownloadSourceItem(urlInfo),
                    }));
                });
                Dispatcher.UIThread.Invoke(() =>
                {
                    LoadRing.IsVisible = false;
                    InstallBtn.IsEnabled = true;
                });
            }
            else
            {
                DialogHost.Show(new DialogInfo()
                {
                    Title = "发生错误",
                    Content = "该版本无法获取到对应下载地址",
                    CloseButtonText = "确定",
                    CloseAction = () =>
                    {
                        GlobalModel.MainWindow.CloseDraw();
                    }
                });
            }
        });
    }

    private void InstallBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (InstallFolder.Items.Count <= 0)
        {
            var dialog = new DialogAddGameFolderContent();
         
            DialogHost.Show(new DialogInfo()
            {
                Title = "添加游戏根目录",
                Content = dialog,
                CloseButtonText = "添加",
                SecondaryButtonText = "取消",
                AccountButton = DialogButtons.CloseButton,
                CloseAction = () =>
                {
                    if (Directory.Exists(dialog.FolderPath))
                    {
                        var name = string.IsNullOrEmpty(dialog.FolderName)
                            ? Path.GetFileName(Path.GetDirectoryName(dialog.FolderPath))
                            : dialog.FolderName;
                    
                        GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo()
                        {
                            GameFolderPath = dialog.FolderPath,
                            GameFolderName = name
                        });
                        GlobalModel.Config.Data.GameFolderSelIndex = 0;
                        GlobalModel.Config.Save();
                    
                        UpdateUI();

                        TaskDownloadGameItem.Install(BuildInfo, Sources[SourceSelBox.SelectedIndex].Url,
                            GlobalModel.Config.Data.GameFolders[InstallFolder.SelectedIndex].GameFolderPath,
                            InstallName.Text);
        
                        GlobalModel.MainWindow.CloseDraw();
                    }
                }
            });
        }
        else
        {
            TaskDownloadGameItem.Install(BuildInfo, Sources[SourceSelBox.SelectedIndex].Url,
                GlobalModel.Config.Data.GameFolders[InstallFolder.SelectedIndex].GameFolderPath, InstallName.Text);
        
            GlobalModel.MainWindow.CloseDraw();
        }
    }
}