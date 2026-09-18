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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Interface.Download;
using BedrockBoot.Views.Control.Widgets;

namespace BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

public partial class ResultDescription : UserControl
{
    private readonly IDownloadResult _service;
    public Action? NotFountDescription;

    public ResultDescription()
    {
        InitializeComponent();
    }

    public ResultDescription(IDownloadResult service) : this()
    {
        _service = service;
        UpdateUI();
    }

    private async Task UpdateUI()
    {
        PreviewList.Children.Clear();
        if (_service.SearchInfo.Images is { Count: > 0 })
        {
            PreviewCard.IsVisible = true;
            foreach (var image in _service.SearchInfo.Images)
                PreviewList.Children.Add(new LocalImageRenderWidget(image) { Width = 290 });
        }

        var controls = await _service.DescriptionControls();
        if (controls == null || controls.Count <= 0)
        {
            NotFountDescription?.Invoke();
            return;
        }

        ;
        DescControls.Children.AddRange(controls);
        DescCard.IsVisible = controls.Count > 0;
    }
}