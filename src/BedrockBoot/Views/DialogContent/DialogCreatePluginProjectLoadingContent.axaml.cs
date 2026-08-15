using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Pack.Plugin.Develop;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entry;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogCreatePluginProjectLoadingContent : UserControl
{
    public DialogCreatePluginProjectLoadingContent()
    {
        InitializeComponent();
    }
    
    public DialogCreatePluginProjectLoadingContent(PackConfig conf):this()
    {
        Task.Run(() =>
        {
            DevelopCore.CreatePluginProject(conf, (s, e) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    ProgressText.Text = e;
                    ProgressBar.Value = s;
                });
            });
            DialogHost.Close();
        });
    }
}