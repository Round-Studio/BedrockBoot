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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using BedrockBoot.Base.Entry.Info.News;

namespace BedrockBoot.Views.Control.Items;

public partial class NewsItem : UserControl
{
    private static readonly BlurEffect SharedBlur = new() { Radius = 8 };

    public NewsItem()
    {
        InitializeComponent();
    }
    public NewsItem(MojangNewsManifest.PatchNoteEntry info) : this()
    {
        ImageRender.Update("https://launchercontent.mojang.com" + info.Image.Url);
        ItemTitle.Text = info.Title;
        ItemSubTitle.Text = info.ShortText;
        HyperlinkButton.NavigateUri = new Uri($"https://minecraft.wiki/?search={info.Title.Replace(" ","+")}");
        ItemTimeAgo.Text = GetSmartDiff(info.Date);
    }

    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        ImageRender.Effect = SharedBlur;
        ImageRender.Margin = new Thickness(-20);
    }

    private void InputElement_OnPointerExited(object? sender, PointerEventArgs e)
    {
        ImageRender.Effect = null;
        ImageRender.Margin = new Thickness(0);
    }

    string GetSmartDiff(string dateString)
    {
        if (!DateTime.TryParse(dateString, out DateTime targetDate))
            return "无效日期";

        TimeSpan diff = DateTime.Now - targetDate;
    
        if (diff.TotalSeconds < 0)
            return "未来日期";

        if (diff.TotalDays >= 365)
        {
            double years = diff.TotalDays / 365.2425;
            return $"{years:F0}年前";
        }
        else if (diff.TotalDays >= 30)
        {
            double months = diff.TotalDays / 30.436875;
            return $"{months:F0}月前";
        }
        else if (diff.TotalDays >= 1)
        {
            return $"{(int)diff.TotalDays}天前";
        }
        else if (diff.TotalHours >= 1)
        {
            return $"{(int)diff.TotalHours}小时前";
        }
        else if (diff.TotalMinutes >= 1)
        {
            return $"{(int)diff.TotalMinutes}分钟前";
        }
        else
        {
            return $"{(int)diff.TotalSeconds}秒前";
        }
    }
}
