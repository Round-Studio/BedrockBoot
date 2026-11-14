using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game.Pack.Capes;
using BedrockBoot.Standard.Entity.Game.Pack.Capes.Manifest;

namespace BedrockBoot.Views.Control.Items.Cape;

public partial class CapeFileItem : UserControl
{
    private readonly CapesPackUnit _unit;
    private readonly CapeDefinition _capeDefinition;

    public CapeFileItem()
    {
        InitializeComponent();
    }
    public CapeFileItem(CapesPackUnit unit,CapeDefinition capeDefinition):this()
    {
        _unit = unit;
        _capeDefinition = capeDefinition;
        SettingExpander.Header = unit.GetLanguage(capeDefinition.LocalizationName);
        SettingExpander.Description = $"品质：{CapeTypeHelper.GetTypeName(CapeTypeHelper.GetNameToType(capeDefinition.Rarity!))}";
        SettingExpander.ImageIcon = CapeImageHelper.PatchCapeIcon(Path.Combine(_unit.Folder!, capeDefinition.Texture));
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}