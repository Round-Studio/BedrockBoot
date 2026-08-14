using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Info.Develop;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using NotImplementedException = System.NotImplementedException;

namespace BedrockBoot.Views.Control.Items;

public partial class PluginProjectItem : UserControl
{
    private readonly ProjectInfo _project;

    public PluginProjectItem()
    {
        InitializeComponent();
    }
    
    public Action<string> OnDeleteProject { get; set; } = s => throw new NotImplementedException();
    public Action<string> OnOpenProject { get; set; } = s => throw new NotImplementedException();

    public PluginProjectItem(ProjectInfo project) : this()
    {
        _project = project;
        DescriptionBox.Text = project.PackInfo.PackDescription;
        NameBox.Text = project.PackInfo.PackName;
        VersionBox.Text = project.PackInfo.PackVersion;
    }

    private void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new()
        {
            Title = "删除项目",
            Content = "删除此项目仅会在列表中删除，并不会删除本地文件",
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                OnDeleteProject?.Invoke(_project.ProjectPath);
            }
        });
    }

    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        OnOpenProject.Invoke(_project.ProjectPath);
    }
}