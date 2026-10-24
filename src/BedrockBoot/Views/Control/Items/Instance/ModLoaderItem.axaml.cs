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

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Standard.Interface.ModLoader;
using NotImplementedException = System.NotImplementedException;

namespace BedrockBoot.Views.Control.Items.Instance;

public partial class ModLoaderItem : ISetting
{
    private readonly ImageLoader _imageLoader = ImageLoader.Shared;
    private readonly VersionConfig _instance;
    private readonly IModsLoader _loader;
    private bool _canInstall;

    public ModLoaderItem()
    {
        InitializeComponent();
    }

    public ModLoaderItem(VersionConfig instance, IModsLoader loader) : this()
    {
        _instance = instance;
        _loader = loader;
        _loader.OnUpdate = async () =>
        {
            await UpdateUi();
        };
        LoaderCard.Click += LoaderCard_OnClick;
        DeleteBtn.Click += DeleteBtn_OnClick;
        _ = UpdateUi();
    }

    public async Task UpdateUi()
    {
        IsEdit = false;
        _loader.InitLoader(_instance);

        LoaderName.Text = _loader.LoaderName;
        LoaderCard.Description = _loader.LoaderDescription;
        if (_loader.IsInstalled())
        {
            var version = _loader.GetInstalledVersion();
            LoaderInstallStatus.Text = !_loader.IsInstalled() ? "未安装" : version;
            LoaderInstallStatus.IsVisible = !string.IsNullOrEmpty(version);
        }
        if (!string.IsNullOrEmpty(_loader.IconUri))
        {
            LoaderCard.IsFontIcon = false;
            LoaderCard.ImageIcon = await _imageLoader.LoadIconAsync(_loader.IconUri, 64);
        }

        if (!_loader.IsInstalled())
        {
            _canInstall = await _loader.ApplicableInstance();
            LoadingRing.IsVisible = false;
            NotApplicableLabel.IsVisible = !_canInstall;
        }
        else
        {
            _canInstall = false;
            DeleteBtn.IsVisible = _loader.CanRemove;
            IsEnableToggle.IsVisible = _loader.IsAllowDisabling;
            IsEnableToggle.IsChecked = _loader.GetIsEnabled();
            LoadingRing.IsVisible = false;
        }

        IsEdit = true;
    }

    private void LoaderCard_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_loader.IsInstalled())
            _loader.ViewInfo();
        else if (_canInstall)
            _loader.Install();
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_loader.IsInstalled() && _loader.CanRemove)
            _loader.Remove();
    }

    private void IsEnableToggle_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            var enable = (bool)IsEnableToggle.IsChecked!;
            _loader.SetIsEnabled(enable);
        }
    }
}