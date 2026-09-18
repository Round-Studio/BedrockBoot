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

using Avalonia.Controls;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.ContentView;
using BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceSave : UserControl
{
    public InstanceSave()
    {
        IsEdit = false;
        InitializeComponent();
    }

    public InstanceSave(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        UpdateUI();
    }

    public VersionConfig VersionInfo { get; set; }
    public bool IsEdit { get; set; }

    private void UpdateUI()
    {
        OnNavigatedTo(true);
    }

    public void OnNavigatedTo(bool isSavesView, object page = null)
    {
        if (isSavesView)
            NavigationFrame.NavigateTo(new SavesView(VersionInfo)
            {
                EditAction = info => OnNavigatedTo(false, new LevelSettingsRoot(info)
                {
                    BackAction = () =>
                        OnNavigatedTo(true)
                })
            });
        else
            NavigationFrame.NavigateTo((UserControl)page);
    }
}