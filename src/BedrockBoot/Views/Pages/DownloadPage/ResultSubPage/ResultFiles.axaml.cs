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
        UpdateUI();
    }

    private async Task UpdateUI()
    {
        if (_service.SearchInfo.ResourceType == SearchResourceType.ResourcePack)
        {
            var files = await new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey)
                .GetModFilesAsync(_service.SearchInfo.Id);

            files.Data.ForEach(f => { FilesList.Children.Add(new CurseForgeModBuildFileItem(f)); });
        }
    }
}