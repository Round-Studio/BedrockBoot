using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Items;

public partial class GameResourcePackItem : UserControl
{
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

    public void UpdateUI()
    {
        if (ResourcePackManifest == null) return;

        try
        {
            if (!string.IsNullOrEmpty(ResourcePackManifest.PackIcon) && File.Exists(ResourcePackManifest.PackIcon))
            {
                // 释放旧的 Bitmap 资源，避免内存泄漏
                if (Card.ImageIcon is IDisposable disposable) disposable.Dispose();

                using var stream = File.OpenRead(ResourcePackManifest.PackIcon);
                Card.ImageIcon = new Bitmap(stream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load pack icon: {ex.Message}");
        }

        PackName.MinecraftText = ResourcePackManifest.Header.Name;
        PackDescription.MinecraftText = ResourcePackManifest.Header.Description;
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
}