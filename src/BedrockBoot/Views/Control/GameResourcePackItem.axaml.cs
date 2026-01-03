using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control;

public partial class GameResourcePackItem : UserControl
{
    public ResourcePackManifest ResourcePackManifest { get; set; }
    public Action RefreshCallBack;
    public GameResourcePackItem()
    {
        InitializeComponent();
    }

    public GameResourcePackItem(ResourcePackManifest maf, bool isImport = false) : this()
    {
        ResourcePackManifest = maf;

        Update();
        ControlBox.IsVisible = !isImport;
    }

    public void Update()
    {
        Card.ImageIcon = new Bitmap(ResourcePackManifest.PackIcon!);
        PackName.MinecraftText = ResourcePackManifest.Header.Name;
        PackDescription.MinecraftText = ResourcePackManifest.Header.Description;
        PackType.Text = ResourcePackManifest.PackType.ToString();
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo()
        {
            Title = "删除资源",
            Content = "您确定要删除吗，这将永远无法恢复。",
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                DialogHost.Show(new DialogInfo()
                {
                    Title = "删除资源",
                    Content = "正在删除资源"
                });
                Task.Run(() =>
                {
                    try
                    {
                        Directory.Delete(ResourcePackManifest.PackRootPath, true);
                    }
                    catch
                    {
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
                            {
                                Title = "错误",
                                Message = "删除失败"
                            });
                        });
                    }

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        DialogHost.Close();
                        RefreshCallBack?.Invoke();
                    });
                });
            }
        });
    }
}