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

        ProjectNameText.Text = string.IsNullOrEmpty(conf.ProjectName) ? conf.ProjectPath : conf.ProjectName;
        ProjectPathText.Text = conf.ProjectPath;

        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        CopyTextBox.Text = string.IsNullOrEmpty(exePath)
            ? $"BedrockBoot -debug \"{conf.ProjectPath}\""
            : $"\"{exePath}\" -debug \"{conf.ProjectPath}\"";
    }
}