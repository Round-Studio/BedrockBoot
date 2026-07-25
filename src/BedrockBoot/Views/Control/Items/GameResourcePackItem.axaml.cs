using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.DialogContent.Skin;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Items;

public partial class GameResourcePackItem : UserControl
{
	private ImageLoader _imageLoader = new ImageLoader();
    public GameResourcePackItem()
    {
        InitializeComponent();
    }

    public GameResourcePackItem(ResourcePackManifest maf, bool isImport = false) : this()
    {
        ResourcePackManifest = maf;
        UpdateUI();
        ControlBox.IsVisible = !isImport;
    }

    private static I18nManager i18n => I18nManager.Instance;
    public Action? RefreshCallBack { get; set; }
    public ResourcePackManifest ResourcePackManifest { get; set; } = null!;
    protected override void OnUnloaded(RoutedEventArgs e)
    {
	    base.OnUnloaded(e);
	    _imageLoader.Dispose();
    }

    public async Task UpdateUI()
    {
        if (ResourcePackManifest == null) return;

        try
        {
            if (ResourcePackManifest.PackIconBytes != null)
            {
                if (Card.ImageIcon is IDisposable disposable) disposable.Dispose();

                using var ms = new MemoryStream(ResourcePackManifest.PackIconBytes);
                Card.ImageIcon = new Bitmap(ms);
            }
            else if (!string.IsNullOrEmpty(ResourcePackManifest.PackIcon))
            {
                if (Card.ImageIcon is IDisposable disposable) disposable.Dispose();

                Card.ImageIcon = await _imageLoader.LoadIconAsync(ResourcePackManifest.PackIcon);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to load pack icon: {ex.Message}");
        }

        ViewBtn.IsVisible = ResourcePackManifest.PackType == ResourcePackType.Skin;
        PackName.MinecraftText = ResourcePackManifest.Header.Name;
        PackDescription.MinecraftText = string.IsNullOrEmpty(ResourcePackManifest.Header.Description)
            ? "该包还没有介绍..."
            : ResourcePackManifest.Header.Description;
        if (!string.IsNullOrEmpty(ResourcePackManifest.Header.Version))
            PackVersion.Text = ResourcePackManifest.Header.Version;
        GameVersion.IsVisible = !string.IsNullOrEmpty(ResourcePackManifest.Header.Version);
        if (!string.IsNullOrEmpty(ResourcePackManifest.Header.MinEngineVersion))
            GameVersion.Text = $"Minecraft {ResourcePackManifest.Header.MinEngineVersion}";
        GameVersion.IsVisible = !string.IsNullOrEmpty(ResourcePackManifest.Header.MinEngineVersion);
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Resource.Delete.Title"],
            Content = $"{i18n["Instance.Resource.Delete.Content"]}\n{i18n["Common.Action.Irreversible"]}",
            CloseButtonText = i18n["MainWindow.Common.Confirm"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = () =>
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = i18n["Instance.Resource.Delete.Title"],
                    Content = i18n["Instance.Resource.Delete.Processing"]
                });

                Task.Run(async () =>
                {
                    var success = false;
                    try
                    {
                        if (Directory.Exists(ResourcePackManifest.PackRootPath))
                        {
                            Directory.Delete(ResourcePackManifest.PackRootPath, true);
                            success = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                            {
                                Title = i18n["MainWindow.Dialog.Error.Title"],
                                Message = $"{i18n["Instance.Resource.Delete.Error"]}: {ex.Message}",
                                NoticeType = NoticeType.Error
                            });
                        });
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        DialogHost.Close();
                        if (success) RefreshCallBack?.Invoke();
                    });
                });
            }
        });
    }

    private void ViewBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new()
        {
            Title = "预览皮肤包",
            Content = new DialogSkinPackViewerContent(ResourcePackManifest.PackRootPath),
            CloseButtonText = "完成"
        });
    }
}