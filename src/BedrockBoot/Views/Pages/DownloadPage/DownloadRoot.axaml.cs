using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;

namespace BedrockBoot.Views.Pages.DownloadPage;

public partial class DownloadRoot : BedrockBootPage
{
    public DownloadRoot()
    {
        InitializeComponent();
        
        MainFrame.NavigateTo(new DownloadSearch());
    }
}