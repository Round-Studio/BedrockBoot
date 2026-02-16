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
}