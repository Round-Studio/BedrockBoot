using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Interface.Download;

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
    }
}