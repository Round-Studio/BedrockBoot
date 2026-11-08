using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Standard.Entity.Game.Mods.Arke;

namespace BedrockBoot.Views.Control.Items.Instance.Mods;

public partial class ArkeShaderPackItem : UserControl
{
    private readonly ArkeShaderPackInfo _info;
    private readonly Action<string> _deleteAction;

    public ArkeShaderPackItem()
    {
        InitializeComponent();
    }

    public ArkeShaderPackItem(ArkeShaderPackInfo info, Action<string> deleteAction) : this()
    {
        _info = info;
        _deleteAction = deleteAction;

        Header.MinecraftText = info.PackName;
        Card.Description = FormatSize(info.PackSize);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "0 B";

        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int counter = 0;
        decimal number = bytes;
        
        while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }

        string format = number < 10 && counter > 0 ? "0.##" : "0.#";
        return $"{number.ToString(format)} {suffixes[counter]}";
    }

    private void SettingCard_OnClick(object? sender, RoutedEventArgs e)
    {
        _deleteAction.Invoke(_info.PackPath);
    }
}