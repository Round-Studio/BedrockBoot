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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Interface.Download;
using BedrockBoot.Views.Control.Widgets;

namespace BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

public partial class ResultDescription : UserControl
{
    private readonly IDownloadResult _service;
    private bool _loaded;
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

        DescControls.Children.AddRange(controls);
        DescCard.IsVisible = controls.Count > 0;
        _loaded = true;
    }

    /// <summary>
    /// 翻译当前控件树里的全部文本。控件结构与样式不动，只改写 Run.Text。
    /// 还原走 RestoreOriginal 重新渲染原始 HTML（原地改写不可逆）。
    /// </summary>
    public async Task<bool> TranslateContentAsync(
        Func<IReadOnlyList<string>, Task<IReadOnlyList<string>?>> translateBatch,
        Action<int, int>? progress = null)
    {
        if (!_loaded || DescControls.Children.Count == 0) return false;

        return await ControlTextTranslator.TranslateControlsAsync(
            DescControls.Children.ToList(), translateBatch, progress);
    }

    /// <summary>还原为原文渲染：从服务重新取 HTML 重建控件树（翻译是原地改写，无法直接回退）。</summary>
    public async void RestoreOriginal()
    {
        if (!_loaded) return;

        DescControls.Children.Clear();
        var controls = await _service.DescriptionControls();
        if (controls == null || controls.Count <= 0) return;

        DescControls.Children.AddRange(controls);
    }
}
