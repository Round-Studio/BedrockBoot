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

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Pack.LeviLamina;

namespace BedrockBoot.Views.DialogContent.Loader.LeviLamina;

public partial class DialogChooseLeviLaminaModInstallInstanceContent : UserControl
{
    private readonly LeviModEntry _entry;
    private readonly LeviModVersion _version;
    private readonly string _label;
    public bool IsOnlyDownload => DownloadType.SelectedIndex == 1;

    public string? SavePath => IsOnlyDownload ? _selectPath :
        _ableInstance.Count > 0 ? _ableInstance[ComboBox.SelectedIndex].conf.VersionPath : string.Empty;

    private string? _selectPath;
    private List<(VersionConfig conf, string llVersion)> _ableInstance;

    public DialogChooseLeviLaminaModInstallInstanceContent()
    {
        InitializeComponent();
    }

    public DialogChooseLeviLaminaModInstallInstanceContent(LeviModEntry entry, LeviModVersion version, string label) :
        this()
    {
        _entry = entry;
        _version = version;
        _label = label;
        UpdateUi();
    }

    public void UpdateUi()
    {
        var deps = LeviModsSource.GetVariantAsset(_version, _label)?.Deps ?? new Dictionary<string, string>();
        DepsList.IsVisible = deps.Count > 0;
        deps.ToList().ForEach(dep =>
        {
            DepsList.Children.Add(new TextBlock() { Text = $"{FormatDepName(dep.Key)} {dep.Value}" });
        });

        var nowChooseFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex]
            .GameFolderPath;
        var instances = GameInfoHelper.GetVersionConfigs(nowChooseFolder);
        _ableInstance = new List<(VersionConfig conf, string llVersion)>();
        instances.ForEach(instance =>
        {
            var loader = new Models.Pack.Game.Loaders.LoaderInstance.LeviLamina();
            loader.InitLoader(instance);

            if (loader.IsInstalled())
                _ableInstance.Add((instance, loader.GetInstalledVersion()));
        });

        ComboBox.ItemsSource = _ableInstance.Select(x =>
            $"{x.conf.Info.VersionName} ({x.conf.Info.Version}) - LeviLamina {x.llVersion}");
        NotFound.IsVisible = _ableInstance.Count <= 0;
        ComboBox.IsVisible = _ableInstance.Count > 0;
        if (_ableInstance.Count > 0)
        {
            ComboBox.SelectedIndex = 0;
        }
    }

    // 依赖键形如 github.com/LiteLDev/LeviLamina#client，展示时去掉主机前缀与变体标签
    private static string FormatDepName(string depKey)
    {
        var name = depKey?.Split('/').Last() ?? string.Empty;
        var hashIndex = name.IndexOf('#');
        return hashIndex > 0 ? name.Substring(0, hashIndex) : name;
    }

    private async void SelectFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(Models.Global.GlobalModel.MainWindow);
        if (topLevel == null)
            return;

        var storageProvider = topLevel.StorageProvider;

        var options = new FilePickerSaveOptions
        {
            Title = I18nManager.Instance["UI.Dialog.SaveZipFile"],
            SuggestedFileName = $"{_entry?.Name}-{_version?.Version}.zip",
            DefaultExtension = "zip",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new(I18nManager.Instance["FileType.Zip"])
                {
                    Patterns = new[] { "*.zip" }
                }
            }
        };

        var file = await storageProvider.SaveFilePickerAsync(options);
        _selectPath = file?.TryGetLocalPath();
    }
}
