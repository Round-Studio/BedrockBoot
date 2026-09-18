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
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Core.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Control.Items;

public partial class GameFolderItem : UserControl
{
    public GameFolderItem()
    {
        InitializeComponent();
    }

    public GameFolderItem(GameFolderInfo info) : this()
    {
        GameFolderInfo = info;
        UpdateUI();
    }

    private static I18nManager i18n => I18nManager.Instance;
    public GameFolderInfo GameFolderInfo { get; set; } = null!;

    private void UpdateUI()
    {
        FolderPathBox.Text = GameFolderInfo.GameFolderPath;
        FolderNameBox.Text = GameFolderInfo.GameFolderName;
        FolderType.Text = GameFolderInfo.GameFolderType.ToString();
        FolderType.IsVisible = GameFolderInfo.GameFolderType != GameFolderType.BedrockBoot;
    }

    /// <summary>
    ///     在资源管理器中打开该目录
    /// </summary>
    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(GameFolderInfo.GameFolderPath)) return;

        try
        {
            OpenFolderHelper.Open(GameFolderInfo.GameFolderPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to open folder: {ex.Message}");
        }
    }

    /// <summary>
    ///     从配置中移除该目录（不删除物理文件）
    /// </summary>
    private void DeleteFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Setting.Folder.Delete.Title"],
            Content = i18n["Setting.Folder.Delete.Content"],
            SecondaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseButtonText = i18n["MainWindow.Common.Delete"],
            AccountButton = DialogButtons.SecondaryButton,
            CloseAction = () =>
            {
                var folders = GlobalModel.Config.Data.GameFolders;
                var itemToRemove = folders.Find(x =>
                    x.GameFolderPath == GameFolderInfo.GameFolderPath &&
                    x.GameFolderName == GameFolderInfo.GameFolderName);

                if (itemToRemove != null)
                {
                    folders.Remove(itemToRemove);

                    // 如果删除的是当前选中的目录，重置索引
                    if (GlobalModel.Config.Data.GameFolderSelIndex >= folders.Count)
                        GlobalModel.Config.Data.GameFolderSelIndex = Math.Max(0, folders.Count - 1);

                    GlobalModel.Config.Save();

                    // 通知主界面或管理页更新 UI
                    MainManager.Instance.UpdateUI();
                }
            }
        });
    }
}