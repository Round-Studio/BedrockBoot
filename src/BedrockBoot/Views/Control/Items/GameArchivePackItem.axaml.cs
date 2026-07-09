using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class GameArchivePackItem : UserControl
{
    private readonly ResourcePackManifest _info;
    private readonly bool _isAct;
    
    public Action<ResourcePackManifest>? ActiveAction { get; set; }

    public GameArchivePackItem()
    {
        InitializeComponent();
    }
    public GameArchivePackItem(ResourcePackManifest info,bool isAct):this()
    {
        _info = info;
        _isAct = isAct;
        _ = UpdateUi();
    }

    public async Task UpdateUi()
    {
        CheckBtn.IsVisible = !_isAct;
        CancelBtn.IsVisible = _isAct;

        var info = _info;
        
        if (info == null) return;

        try
        {
            if (info.PackIconBytes != null)
            {
                if (Card.ImageIcon is IDisposable disposable) disposable.Dispose();

                using var ms = new MemoryStream(info.PackIconBytes);
                Card.ImageIcon = new Bitmap(ms);
            }
            else if (!string.IsNullOrEmpty(info.PackIcon))
            {
                if (Card.ImageIcon is IDisposable disposable) disposable.Dispose();

                Card.ImageIcon = await ImageLoader.LoadIconAsync(info.PackIcon);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to load pack icon: {ex.Message}");
        }

        PackName.MinecraftText = info.Header.Name;
        PackDescription.MinecraftText = string.IsNullOrEmpty(info.Header.Description)
            ? "该包还没有介绍..."
            : info.Header.Description;
        if (!string.IsNullOrEmpty(info.Header.Version))
            PackVersion.Text = info.Header.Version;
        GameVersion.IsVisible = !string.IsNullOrEmpty(info.Header.Version);
        if (!string.IsNullOrEmpty(info.Header.MinEngineVersion))
            GameVersion.Text = $"Minecraft {info.Header.MinEngineVersion}";
        GameVersion.IsVisible = !string.IsNullOrEmpty(info.Header.MinEngineVersion);
    }

    private void ActBtn_Click(object? sender, RoutedEventArgs e)
    {
        ActiveAction?.Invoke(_info);
    }
}