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
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Desktop;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Windows.SystemMethod;

public partial class ImportResourcePack : Window
{
    private List<VersionConfig> _currentGames;
    private string _filePath;

    public ImportResourcePack()
    {
        InitializeComponent();

        InitGameFolders();

        if (Program.Args != null && Program.Args.Contains("-open"))
        {
            var index = Program.Args.FindIndex(a => a == "-open");
            if (index + 2 < Program.Args.Count)
            {
                _filePath = Program.Args[index + 2];
                LoadPack(_filePath);
            }
        }
    }

    private void InitGameFolders()
    {
        var folders = GlobalModel.Config.Data.GameFolders;

        if (folders == null || folders.Count <= 0) return;

        FolderComboBox.ItemsSource = folders.Select(f => $"{f.GameFolderName} - {f.GameFolderPath}").ToList();

        // 恢复上次选择的索引
        if (GlobalModel.Config.Data.GameFolderSelIndex < folders.Count)
            FolderComboBox.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;
    }

    private void FolderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FolderComboBox.SelectedIndex < 0) return;

        var selectedFolder = GlobalModel.Config.Data.GameFolders[FolderComboBox.SelectedIndex];

        _currentGames = GameInfoHelper.GetVersionConfigs(selectedFolder.GameFolderPath);

        InstanceComboBox.ItemsSource = _currentGames.Select(g => $"{g.Info.VersionName} - {g.Info.Version}").ToList();

        if (_currentGames.Count > 0)
        {
            InstanceComboBox.SelectedIndex = 0;
            ImportButton.IsEnabled = true;
        }
        else
        {
            InstanceComboBox.SelectedIndex = -1;
            ImportButton.IsEnabled = false;
        }
    }

    public void LoadPack(string file)
    {
        _filePath = file;
        LoadingCard.IsVisible = true;
        PacksScrollViewer.IsVisible = false;
        PacksList.Children.Clear();

        Task.Run(() =>
        {
            try
            {
                var manifests = new ResourcePackAnalysis(file).GetPackManifests();

                Dispatcher.UIThread.Invoke(() =>
                {
                    if (manifests == null || manifests.Count == 0)
                    {
                        LoadingCard.BigTitle = "未找到资源";
                        LoadingCard.Message = "所选文件不是有效的资源包。";
                        return;
                    }

                    foreach (var conf in manifests)
                    {
                        var item = new GameResourcePackItem(conf, true);
                        PacksList.Children.Add(item);
                    }

                    LoadingCard.IsVisible = false;
                    PacksScrollViewer.IsVisible = true;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    LoadingCard.BigTitle = "解析失败";
                    LoadingCard.Message = ex.Message;
                });
            }
        });
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_filePath) || InstanceComboBox.SelectedIndex < 0) return;

        LoadingCard.BigTitle = "正在导入";
        LoadingCard.Message = "请稍候，正在处理资源包...（导入完毕将自动关闭窗口）";
        LoadingCard.IsVisible = true;
        PacksScrollViewer.IsVisible = false;
        ImportButton.IsEnabled = false;
        ConfigPanel.IsEnabled = false;

        try
        {
            var selectedGame = _currentGames[InstanceComboBox.SelectedIndex];

            await Task.Run(() =>
            {
                var man = new ResourcePackManager(GameInfoHelper.GetVersionConfig(selectedGame.VersionPath));
                man.GetAllPack();
                man.AddRangePacks(new List<string> { _filePath });
            });

            Close();
        }
        catch (Exception ex)
        {
            LoadingCard.BigTitle = "导入出错";
            LoadingCard.Message = ex.Message;
            ImportButton.IsEnabled = true;
        }
    }
}