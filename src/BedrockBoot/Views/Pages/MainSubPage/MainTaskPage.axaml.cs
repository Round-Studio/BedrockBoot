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
using Avalonia;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainTaskPage : BedrockBootPage
{
    public MainTaskPage()
    {
        InitializeComponent();

        GlobalModel.TaskManager.OnChanged = () => Dispatcher.UIThread.Invoke(UpdateUI);
        UpdateUI();
        Unloaded += (sender, args) => UnLoad();
    }

    public void UpdateUI()
    {
        TaskList.ItemsSource = null;
        TaskViewer.IsVisible = true;
        NoneBox.IsVisible = false;

        if (GlobalModel.TaskManager.Tasks.Count <= 0)
        {
            TaskViewer.IsVisible = false;
            NoneBox.IsVisible = true;
            return;
        }

        var items = new List<Avalonia.Controls.Control>(GlobalModel.TaskManager.Tasks.Count);
        GlobalModel.TaskManager.Tasks.ForEach(task =>
        {
            task.Item.Margin = new Thickness(5);
            items.Add(task.Item);
        });
        // 一次性绑定到 ItemsSource，由 ListBox + VirtualizingStackPanel 按需实例化
        TaskList.ItemsSource = items;
    }

    public void UnLoad()
    {
        TaskList.ItemsSource = null;
        GlobalModel.TaskManager.OnChanged = null;
    }
}