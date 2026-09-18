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

using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Models.Helper;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Control.Items;

public partial class SearchItem : UserControl
{
    private static readonly HttpClient _httpClient = new();
    private ImageLoader _imageLoader = new ImageLoader();
    public SearchItem()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
	    base.OnUnloaded(e);
	    _imageLoader.Dispose();
    }

    public SearchItem(SearchResultItemInfo info) : this()
    {
        SearchResultItemInfo = info;
        ItemName.Text = info.Name;
        Description.Text = info.Description;
        Authors.Text = string.Join(", ", info.Authors);

        if (info.Labels.Count > 0) LabelsPanel.IsVisible = true;

        info.Labels.ForEach(s => LabelsPanel.Children.Add(new LabelBox { Text = s }));

        Update();
    }

    public SearchResultItemInfo SearchResultItemInfo { get; set; }

    private async Task Update()
    {
        var icon = await _imageLoader.LoadImageBrushAsync(SearchResultItemInfo.IconUri);
        if (icon != null)
        {
            Card.IsFontIcon = false;
            Card.ImageIcon = icon;
        }
    }


    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        SearchResultItemInfo.OnClick?.Invoke(SearchResultItemInfo.JsonData);
    }
}