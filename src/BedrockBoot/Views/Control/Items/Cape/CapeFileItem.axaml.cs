/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game.Pack.Capes;
using BedrockBoot.Standard.Entity.Game.Pack.Capes.Manifest;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Items.Cape;

public partial class CapeFileItem : UserControl
{
    private readonly CapesPackUnit _unit;
    private readonly CapeDefinition _capeDefinition;

    public Action? DeleteAction { get; set; }

    public CapeFileItem()
    {
        InitializeComponent();
    }
    public CapeFileItem(CapesPackUnit unit,CapeDefinition capeDefinition):this()
    {
        _unit = unit;
        _capeDefinition = capeDefinition;
        SettingExpander.Header = unit.GetLanguage(capeDefinition.LocalizationName);
        SettingExpander.Description = string.Format(I18nManager.Instance["UI.Item.Cape.Quality"], CapeTypeHelper.GetTypeName(CapeTypeHelper.GetNameToType(capeDefinition.Rarity!)));
        SettingExpander.ImageIcon = CapeImageHelper.PatchCapeIcon(Path.Combine(_unit.Folder!, capeDefinition.Texture));
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["Cape.Item.DeleteFile"],
            Content = string.Format(I18nManager.Instance["Cape.Item.DeleteFile.Confirm"], SettingExpander.Header),
            CloseButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Delete"],
            AccountButton = DialogButtons.PrimaryButton,
            PrimaryAction = () =>
            {
                _unit.RemoveCape(_capeDefinition);
                DeleteAction?.Invoke();
            }
        });
    }
}