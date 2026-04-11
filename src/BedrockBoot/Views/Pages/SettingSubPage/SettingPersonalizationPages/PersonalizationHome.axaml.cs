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
    
    public bool IsEdit;

    public PersonalizationHome()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Personalization.Breadcrumb.Root"],
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new SettingPersonalization())
            },
            new()
            {
                ItemName = I18nManager.Instance["Setting.Personalization.Home.Title"]
            }
        };
        Update();

        IsEdit = true;
    }

    public void Update()
    {
        IsEdit = false;

        HomeTypeBox.SelectedIndex = (int)BedrockBoot.Core.Global.GlobalModel.Config.Data.HomeConfig.HomeType;

        switch (BedrockBoot.Core.Global.GlobalModel.Config.Data.HomeConfig.HomeType)
        {
            case HomeType.None:
                // 这里可以根据类型切换一些描述文本或控件显示
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
            BedrockBoot.Core.Global.GlobalModel.Config.Data.HomeConfig.HomeType = (HomeType)HomeTypeBox.SelectedIndex;
            BedrockBoot.Core.Global.GlobalModel.Config.Save();

            Update();
        }
    }
}