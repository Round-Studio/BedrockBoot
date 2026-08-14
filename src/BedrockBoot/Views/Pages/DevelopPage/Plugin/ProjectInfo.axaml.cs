using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Pack.Plugin.Develop;

namespace BedrockBoot.Views.Pages.DevelopPage.Plugin;

public partial class ProjectInfo : UserControl
{
    public ProjectInfo(string s)
    {
        InitializeComponent();

        var conf = DevelopProjectManager.GetProjectInfo(s);
        CopyTextBox.Text = $"{Process.GetCurrentProcess().MainModule.FileName} -debug \"{conf.ProjectPath}\"";
    }
}