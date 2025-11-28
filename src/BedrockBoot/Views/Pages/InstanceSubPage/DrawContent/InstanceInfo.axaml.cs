using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceInfo : UserControl
{
    public bool IsEdit { get; set; } = false;
    public VersionConfig VersionInfo { get; set; }
    public InstanceInfo()
    {
        IsEdit = false;
        
        InitializeComponent();
    }

    public InstanceInfo(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        
        UpdateUI();
    }

    public void UpdateUI()
    {
        Task.Run(() =>
        {
            IsEdit = false;
            Thread.Sleep(500);

            Dispatcher.UIThread.Invoke(() =>
            {
                InstanceName.Text = VersionInfo.Info.VersionName;

                if (VersionInfo.Config == null)
                    VersionInfo.Config = new();

                InstanceArgs.Text = VersionInfo.Config.OtherCommand;
                InstanceConsole.IsChecked = VersionInfo.Config.IsConsole;
                InstanceEdit.IsChecked = VersionInfo.Config.IsEditModel;
                InstanceIsolated.IsChecked = VersionInfo.Config.IsVersionIsolated;
            });

            Thread.Sleep(500);
            IsEdit = true;
        });
    }

    private void TextTypeConfig_OnChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit)
        {
            if (string.IsNullOrEmpty(InstanceName.Text))
                VersionInfo.Info.VersionName = Path.GetFileName(VersionInfo.VersionPath);
            else VersionInfo.Info.VersionName = InstanceName.Text;
            
            VersionInfo.Config.OtherCommand = InstanceArgs.Text;
            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }
    }

    private void BoolTypeConfig_OnChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            VersionInfo.Config.IsConsole = (bool)InstanceConsole.IsChecked!;
            VersionInfo.Config.IsEditModel = (bool)InstanceEdit.IsChecked!;
            VersionInfo.Config.IsVersionIsolated = (bool)InstanceIsolated.IsChecked!;

            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }
    }
}