using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.GravityCone;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages.GravityConePage;
using BedrockBoot.Views.Pages.MultiplayerPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainGravityConePage : BedrockBootPage
{
    public static NavigationFrame NavigationFrame;

    public MainGravityConePage()
    {
        InitializeComponent();
        NavigationFrame = this.MainFrame;
        
        var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
        if (!File.Exists(Path.Combine(DialogDownloadMultiPlayerDependenceContent.GravityConeExePath, $"gravitycone-cli-{OS.GetSystemType()}-amd64{ext}")) ||
            !File.Exists(Path.Combine(DialogDownloadMultiPlayerDependenceContent.EasyTierPath, $"easytier-cli{ext}")))
            MainFrame.NavigateTo(new MultiplayerDependenceDownload());
        else
        {
            if (GlobalModel.GravityConeClient == null)
            {
                NavigationFrame.NavigateTo(new GravityConeInit());
            }
            else if (GlobalModel.GravityConeClient != null)
            {
                if (GlobalModel.CurrentRoomState != null)
                {
                    NavigationFrame.NavigateTo(new GravityConeRoom());
                }
                else
                {
                    NavigationFrame.NavigateTo(new GravityConeRoot());
                }
            }
        }
    }
}