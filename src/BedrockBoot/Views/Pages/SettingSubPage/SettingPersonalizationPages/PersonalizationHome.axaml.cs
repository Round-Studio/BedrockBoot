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
using Avalonia.Controls;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages
{
    public partial class PersonalizationHome : ISettingPage
    {
        public bool IsEdit;

        public PersonalizationHome()
        {
            InitializeComponent();
            BreadcrumbItem = new List<BreadcrumbItemInfo>
            {
                new()
                {
                    ItemName = I18nManager.Instance["Setting.Personalization.Breadcrumb.Root"],
                    ItemClickAction = info =>
                        MainSettingPage.NavigateTo(new SettingPersonalization())
                },
                new()
                {
                    ItemName = I18nManager.Instance["Setting.Personalization.Home.Title"]
                }
            };
            Update();

            IsEdit = true;
        }

        public void Update()
        {
            IsEdit = false;

            HomeTypeBox.SelectedIndex = (int)GlobalModel.Config.Data.HomeConfig.HomeType;

            switch (GlobalModel.Config.Data.HomeConfig.HomeType)
            {
                case HomeType.None:
                    // 这里可以根据类型切换一些描述文本或控件显示
                    break;
                case HomeType.News:
                    break;
            }

            IsEdit = true;
        }

        private void HomeTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (IsEdit)
            {
                GlobalModel.Config.Data.HomeConfig.HomeType = (HomeType)HomeTypeBox.SelectedIndex;
                GlobalModel.Config.Save();

                Update();
            }
        }
    }
}