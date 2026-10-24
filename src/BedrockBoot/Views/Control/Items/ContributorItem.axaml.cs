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
using Avalonia.Controls;
using Octokit;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Items;

public partial class ContributorItem : UserControl
{
    public ContributorItem()
    {
        InitializeComponent();
    }
    
    public ContributorItem(RepositoryContributor con):this()
    {
        NameText.Content = con.Login;
        NameText.NavigateUri = new Uri(con.HtmlUrl);
        ContributorText.Text = $"{con.Contributions}";
        IconBox.Update(con.AvatarUrl);

        if (con.Login == "Chlna6666")
        {
            NameText.Click += (_,_)=>
            {
                DialogHost.Show(new()
                {
                    Title = "magijj",
                    Content = "?! 蛆蛆 !?",
                    CloseButtonText = "蛆"
                });
            };
        }
    }
}