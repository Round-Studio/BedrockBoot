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
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Pack.Plugin.Develop;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entry;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogCreatePluginProjectLoadingContent : UserControl
{
    public DialogCreatePluginProjectLoadingContent()
    {
        InitializeComponent();
    }
    
    public DialogCreatePluginProjectLoadingContent(PackConfig conf):this()
    {
        Task.Run(() =>
        {
            DevelopCore.CreatePluginProject(conf, (s, e) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    ProgressText.Text = e;
                    ProgressBar.Value = s;
                });
            });
            DialogHost.Close();
        });
    }
}