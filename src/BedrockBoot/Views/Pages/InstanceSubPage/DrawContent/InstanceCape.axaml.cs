using Avalonia.Controls;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.CapeView;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceCape : UserControl
{
    public VersionConfig VersionInfo { get; set; }

    public InstanceCape()
    {
        InitializeComponent();
    }

    public InstanceCape(VersionConfig instance) : this()
    {
        VersionInfo = instance;
        OnNavigatedTo(true);
    }

    public void OnNavigatedTo(bool isCapesView, object page = null)
    {
        if (isCapesView)
            NavigationFrame.NavigateTo(new CapesView(VersionInfo)
            {
                EditAction = info => OnNavigatedTo(false, new CapesPackSetting(VersionInfo, info.FolderName)
                {
                    BackAction = () =>
                        OnNavigatedTo(true)
                })
            });
        else
            NavigationFrame.NavigateTo((UserControl)page);
    }
}
