using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using BedrockBoot.Base.Enum;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Style;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Style.Core;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.View;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages;

public partial class PersonalizationColor : ISetting
{
    public PersonalizationColor()
    {
        InitializeComponent();
        ChooseTheme.SelectedIndex = (int)GlobalModel.Config.Data.StyleConfig.LightThemeType;

        AccentColor.Colors.ForEach(c => ColorsView.Items.Add(new ItemViewItem()
        {
            Content = new Border()
            {
                Background = Brush.Parse(c),
                CornerRadius = new CornerRadius(8),
            },
            Width = 48,
            Height = 48,
            ClipToBounds = true,
        }));

        ColorsView.SelectedIndex = GlobalModel.Config.Data.StyleConfig.AccentColorIndex;
        IsEdit = true;
    }

    private void ChooseTheme_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.StyleConfig.LightThemeType = (ThemeModelEnum)ChooseTheme.SelectedIndex;
            GlobalModel.Config.Save();
            
            App.LoadColor();
        }
    }

    private void ColorsView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.StyleConfig.AccentColorIndex = ColorsView.SelectedIndex;
            GlobalModel.Config.Save();
            
            App.LoadColor();
        }
    }
}