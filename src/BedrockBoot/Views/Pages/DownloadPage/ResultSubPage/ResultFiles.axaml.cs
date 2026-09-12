using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Control.Items.Download;

namespace BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

public partial class ResultFiles : UserControl
{
    private readonly IDownloadResult _service;

    public ResultFiles()
    {
        InitializeComponent();
    }

    public ResultFiles(IDownloadResult service) : this()
    {
        _service = service;
        _ = UpdateUi();
    }

    private List<string> _versions = new();

    private async Task UpdateUi()
    {
        var files = await _service.GetFiles();
        files.ForEach(f =>
        {
            if (!string.IsNullOrEmpty(f.VersionGroup))
            {
                if (!_versions.Contains(f.VersionGroup))
                {
                    _versions.Add(f.VersionGroup);
                    FilesList.Children.Add(new TextBlock()
                    {
                        Text = f.VersionGroup,
                        FontSize = 14,
                        FontWeight = FontWeight.Bold
                    });
                }
            }

            FilesList.Children.Add(new ResourceFileItem(f));
        });
    }
}