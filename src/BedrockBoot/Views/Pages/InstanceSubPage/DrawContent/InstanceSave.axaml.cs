using Avalonia.Controls;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.ContentView;
using BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceSave : UserControl
{
    public InstanceSave()
    {
        IsEdit = false;
        InitializeComponent();
    }

    public InstanceSave(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        UpdateUI();
    }

    public VersionConfig VersionInfo { get; set; }
    public bool IsEdit { get; set; }

    private void UpdateUI()
    {
        OnNavigatedTo(true);
    }

    public void OnNavigatedTo(bool isSavesView, object page = null)
    {
        if (isSavesView)
            NavigationFrame.NavigateTo(new SavesView(VersionInfo)
            {
                EditAction = info => OnNavigatedTo(false, new LevelSettingsRoot(info)
                {
                    BackAction = () =>
                        OnNavigatedTo(true)
                })
            });
        else
            NavigationFrame.NavigateTo((UserControl)page);
    }
}