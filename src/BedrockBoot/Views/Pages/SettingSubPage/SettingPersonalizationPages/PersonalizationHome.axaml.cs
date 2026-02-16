using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Enum;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages;

public partial class PersonalizationHome : ISettingPage
{
    public PersonalizationHome()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "个性化",
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new SettingPersonalization())
            },
            new()
            {
                ItemName = "主页"
            }
        };
        Update();

        IsEdit = true;
    }

    public void Update()
    {
        IsEdit = false;

        HomeTypeBox.SelectedIndex = (int)GlobalModel.Config.Data.HomeConfig.HomeType;

        switch (GlobalModel.Config.Data.HomeConfig.HomeType)
        {
            case HomeType.None:
                break;
            case HomeType.News:
                break;
        }

        IsEdit = true;
    }

    private void HomeTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.HomeConfig.HomeType = (HomeType)HomeTypeBox.SelectedIndex;
            GlobalModel.Config.Save();

            Update();
        }
    }

    private async void AddXmlFile_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = "Choose Xml File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                FilePickerFileTypes.Xml
            }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOptions);

        if (files != null && files.Count > 0)
            foreach (var file in files)
            {
                var filePath = file.Path.LocalPath;

                GlobalModel.Config.Data.HomeConfig.HomeXmlFiles.Add(filePath);
                GlobalModel.Config.Save();
                Update();
            }
        else
            // 用户取消了选择
            Console.WriteLine(@"未选择文件。");
    }
}