using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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

    private async Task UpdateUi()
    {
        var files = await _service.GetFiles();
        files.ForEach(f => FilesList.Children.Add(new ResourceFileItem(f)));
    }
}