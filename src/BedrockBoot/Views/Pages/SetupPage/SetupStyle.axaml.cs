using Avalonia.Controls;
using Avalonia.Styling;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using OnePointUI.Avalonia.Style.Core;

namespace BedrockBoot.Views.Pages.SetupPage;

public partial class SetupStyle : ISetting
{
    public SetupStyle()
    {
        InitializeComponent();
        ChooseThemeBox.SelectedIndex = (int)GlobalModel.Config.Data.StyleConfig.LightThemeType;

        IsEdit = true;
    }

    private void ChooseThemeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.StyleConfig.LightThemeType = (ThemeModelEnum)ChooseThemeBox.SelectedIndex;
            GlobalModel.Config.Save();

            ThemeManager.Instance.SetThemeModel(
                GlobalModel.Config.Data.StyleConfig.LightThemeType == ThemeModelEnum.Light
                    ? ThemeVariant.Light
                    : ThemeVariant.Dark);
        }
    }
}