using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Windows;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages;

public partial class PersonalizationFont : ISettingPage
{
    private const string DefaultPreviewText = "Regular 0123 你好中国";

    public PersonalizationFont()
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
                ItemName = "字体"
            }
        };

        MainFontComboBox.Items.Add(new ComboBoxItem()
        {
            Content = "DINPro",
            FontFamily = new FontFamily("resm:OnePointUI.Avalonia.Assets.Fonts.DinPro.ttf?assembly=OnePointUI.Avalonia#DINPro"),
            Tag = "resm:OnePointUI.Avalonia.Assets.Fonts.DinPro.ttf?assembly=OnePointUI.Avalonia#DINPro"
        });

        MainWindow.InstalledFontNames.ForEach(x =>
        {
            MainFontComboBox.Items.Add(new ComboBoxItem()
            {
                Content = x,
                FontFamily = new(x),
                Tag = x
            });
        });

        FallbackFontComboBox.Items.Add(new ComboBoxItem()
        {
            Content = "DINPro",
            FontFamily = new FontFamily("resm:OnePointUI.Avalonia.Assets.Fonts.DinPro.ttf?assembly=OnePointUI.Avalonia#DINPro"),
            Tag = "resm:OnePointUI.Avalonia.Assets.Fonts.DinPro.ttf?assembly=OnePointUI.Avalonia#DINPro"
        });

        MainWindow.InstalledFontNames.ForEach(x =>
        {
            FallbackFontComboBox.Items.Add(new ComboBoxItem()
            {
                Content = x,
                FontFamily = new(x),
                Tag = x
            });
        });

        LoadConfig();
        IsEdit = true;
    }

    private void LoadConfig()
    {
        var config = GlobalModel.Config.Data.StyleConfig;

        SelectComboBoxItem(MainFontComboBox, config.MainFont, 0);
        SelectComboBoxItem(FallbackFontComboBox, config.FallbackFont, -1);

        // 初始化预览文本框的状态
        PreviewTextInput_TextChanged(null, null);
        
        UpdatePreview();
    }

    private static void SelectComboBoxItem(ComboBox comboBox, string value, int defaultIndex)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (defaultIndex >= 0)
                comboBox.SelectedIndex = defaultIndex;
            return;
        }

        for (var i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is not ComboBoxItem item) continue;
            if (item.Content?.ToString() == value || item.Tag?.ToString() == value)
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }

        if (defaultIndex >= 0)
            comboBox.SelectedIndex = defaultIndex;
    }

    private void MainFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsEdit) return;
        UpdatePreview();
        SaveConfig();
    }

    private void FallbackFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsEdit) return;
        UpdatePreview();
        SaveConfig();
    }

    private void FontSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!IsEdit) return;
        UpdatePreview();
        SaveConfig();
    }

    private void PreviewTextInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void SaveConfig()
    {
        var mainItem = MainFontComboBox.SelectedItem as ComboBoxItem;
        var fallbackItem = FallbackFontComboBox.SelectedItem as ComboBoxItem;

        GlobalModel.Config.Data.StyleConfig.MainFont = mainItem?.Content?.ToString() ?? "DINPro";
        GlobalModel.Config.Data.StyleConfig.FallbackFont = fallbackItem?.Content?.ToString() ?? string.Empty;

        GlobalModel.Config.Save();
    }

    private void UpdatePreview()
    {
        var mainItem = MainFontComboBox.SelectedItem as ComboBoxItem;
        var fallbackItem = FallbackFontComboBox.SelectedItem as ComboBoxItem;

        string mainFont = mainItem?.Tag?.ToString();
        string fallbackFont = fallbackItem?.Tag?.ToString();

        FontFamily combinedFont = Models.Global.GlobalModel.MainWindow.GetFontFamily(mainFont, fallbackFont);

        double fontSize = FontSizeSlider.Value;
        if (fontSize <= 0)
        {
            fontSize = 14;
        }

        // 获取预览文本，如果为空则使用默认文本
        string previewText = string.IsNullOrWhiteSpace(PreviewTextInput.Text) 
            ? DefaultPreviewText 
            : PreviewTextInput.Text;

        PreviewRegular.Text = previewText;
        PreviewRegular.FontFamily = combinedFont;
        PreviewRegular.FontSize = fontSize;

        PreviewItalic.Text = previewText;
        PreviewItalic.FontFamily = combinedFont;
        PreviewItalic.FontSize = fontSize;

        PreviewBold.Text = previewText;
        PreviewBold.FontFamily = combinedFont;
        PreviewBold.FontSize = fontSize;

        PreviewBoldItalic.Text = previewText;
        PreviewBoldItalic.FontFamily = combinedFont;
        PreviewBoldItalic.FontSize = fontSize;
    }
}