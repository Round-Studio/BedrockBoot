using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using BedrockBoot.Base.Enum;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Style;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.View;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages;

public partial class PersonalizationColor : ISettingPage
{
    
    public bool IsEdit;

    public PersonalizationColor()
    {
        InitializeComponent();
        
        // 面包屑导航国际化
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
                ItemName = I18nManager.Instance["Setting.Personalization.Color.Title"]
            }
        };

        // 还原主题选择索引
        ChooseTheme.SelectedIndex = (int)BedrockBoot.Core.Global.GlobalModel.Config.Data.StyleConfig.LightThemeType;

        // 渲染强调色色块列表
        AccentColor.Colors.ForEach(c => ColorsView.Items.Add(new ItemViewItem
        {
            Content = new Border
            {
                Background = Brush.Parse(c),
                CornerRadius = new CornerRadius(8)
            },
            Width = 48,
            Height = 48,
            ClipToBounds = true
        }));

        ColorsView.SelectedIndex = BedrockBoot.Core.Global.GlobalModel.Config.Data.StyleConfig.AccentColorIndex;
        IsEdit = true;
    }

    private void ChooseTheme_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            BedrockBoot.Core.Global.GlobalModel.Config.Data.StyleConfig.LightThemeType = (ThemeModelEnum)ChooseTheme.SelectedIndex;
            BedrockBoot.Core.Global.GlobalModel.Config.Save();

            App.LoadColor();
        }
    }

    private void ColorsView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            BedrockBoot.Core.Global.GlobalModel.Config.Data.StyleConfig.AccentColorIndex = ColorsView.SelectedIndex;
            BedrockBoot.Core.Global.GlobalModel.Config.Save();

            App.LoadColor();
        }
    }
}