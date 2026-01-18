using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawImportOtherLauncherContent : UserControl
{
    public DrawImportOtherLauncherContent()
    {
        InitializeComponent();

        var show = true;
        PathsList.OtherLauncher.ForEach(x =>
        {
            if (File.Exists(x.ConfigFile))
            {
                show = false;
                var item = new SettingCard()
                {
                    IsClickable = true,
                    IsNotFontIcon = true,
                    ImageIcon = new Bitmap(AssetLoader.Open(new Uri(x.IconUrl))),
                    Header = x.Name,
                    Description = $"从 {x.Name} 启动器中导入..."
                };
                item.Click += (sender, args) => x.OnImport?.Invoke(x.ConfigFile);
                LaunchersBox.Children.Add(item);
            }
        });
        NoneBox.IsVisible = show;
    }
}