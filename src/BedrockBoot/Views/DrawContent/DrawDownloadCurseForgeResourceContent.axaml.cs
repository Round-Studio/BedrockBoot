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
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Pages.DownloadSubPage.CurseForge;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawDownloadCurseForgeResourceContent : UserControl
{
	private ImageLoader _imageLoader = new ImageLoader();
    public CurseForgeResponse.ModData ModData;

    public DrawDownloadCurseForgeResourceContent()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
	    base.OnUnloaded(e);
	    _imageLoader.Dispose();
    }

    public DrawDownloadCurseForgeResourceContent(CurseForgeResponse.ModData mod) : this()
    {
        ModData = mod;
        Update();
    }

    public void Update()
    {
        PackName.Text = ModData.Name;
        PackDescription.Text = ModData.Summary;
        RankingBox.Text = ModData.GamePopularityRank.ToString();
        DownCountBox.Text = ModData.DownloadCount.ToString();
        InstanceFrame.NavigateTo(new CurseForgePackBuildFile(ModData));

        ModData.Categories.ForEach(cat =>
        {
            TypesBox.Children.Add(new LabelBox
            {
                Text = cat.Name,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2.5)
            });
        });

        Task.Run(() =>
        {
            var image =_imageLoader.LoadImageBrushAsync(ModData.Logo.ThumbnailUrl).Result;
            Dispatcher.UIThread.Invoke(() =>
            {
                NullImage.IsVisible = false;
                IconBox.Background = new ImageBrush
                {
                    Source = image
                };
            });
        });
    }
}