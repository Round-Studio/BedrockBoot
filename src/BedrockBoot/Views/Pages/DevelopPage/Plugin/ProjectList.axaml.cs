using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Plugin.Develop;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Pages.MainSubPage;
using Round.SDK.Entity;

namespace BedrockBoot.Views.Pages.DevelopPage.Plugin;

public partial class ProjectList : UserControl
{
    public ProjectList()
    {
        InitializeComponent();

        UpdateUI();
    }

    private void UpdateUI()
    {
        DevelopProjectManager.Init();
        NoneCard.IsVisible = DevelopProjectManager.Projects.Count == 0;
        ProjectStackPanel.Children.Clear();
        
        foreach (var project in DevelopProjectManager.Projects)
        {
            var projectCard = new PluginProjectItem(project)
            {
                OnDeleteProject = path =>
                {
                    var file = Path.Combine(PathsList.ConfigFolderPath, "projects.json");
                    var conf = new ConfigEntity<List<string>>(file);
                    if (conf.Data == null)
                        conf.Data = new();

                    conf.Data.Remove(path);
                    conf.Save();
                    
                    DevelopProjectManager.Init();
                    UpdateUI();
                },
                OnOpenProject = (s) =>
                {
                    GlobalModel.MainWindow.OpenDraw(new ProjectInfo(s), "项目详情");
                }
            };
            ProjectStackPanel.Children.Add(projectCard);
        }
    }
}