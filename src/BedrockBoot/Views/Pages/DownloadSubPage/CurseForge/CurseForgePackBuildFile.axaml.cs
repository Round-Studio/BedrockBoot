using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;

namespace BedrockBoot.Views.Pages.DownloadSubPage.CurseForge;

public partial class CurseForgePackBuildFile : UserControl
{
    public CurseForgeResponse.ModData ModData;
    public CurseForgePackBuildFile()
    {
        InitializeComponent();
    }

    public CurseForgePackBuildFile(CurseForgeResponse.ModData mod) : this()
    {
        ModData = mod;
        
        Update();
    }

    private void Update()
    {
        Console.WriteLine($"查看模组详细信息：{ModData.Id}");

        Task.Run(() =>
        {
            var files = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey)
                .GetModFilesAsync(ModData.Id,
                    pageSize: 50).Result;

            Dispatcher.UIThread.Invoke(() =>
            {
                files.Data.ForEach(f =>
                {
                    List.Children.Add(new TextBlock()
                    {
                        Text = f.DisplayName
                    });
                });

                LoadingRing.IsVisible = false;
            });
        });
    }
}