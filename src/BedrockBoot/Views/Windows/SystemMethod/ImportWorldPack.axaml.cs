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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Desktop;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.Control.Items;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Views.Windows.SystemMethod;

public partial class ImportWorldPack : Window
{
    private ArchiveManifest? _currentArchiveManifest;
    private List<VersionConfig> _currentGames = new();
    private string _filePath = string.Empty;

    public ImportWorldPack()
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
            InstanceComboBox.SelectedIndex = 0;
        else
            ClearUserList();
    }

    private void InstanceComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (InstanceComboBox.SelectedIndex < 0)
        {
            ClearUserList();
            return;
        }

        var selectedGame = _currentGames[InstanceComboBox.SelectedIndex];
        var checker = new ArchiveCheck(selectedGame);
        _currentArchiveManifest = checker.Check();

        if (_currentArchiveManifest?.Manifest != null && _currentArchiveManifest.Manifest.Count > 0)
        {
            UserComboBox.ItemsSource = _currentArchiveManifest.Manifest.Keys.ToList();
            UserComboBox.SelectedIndex = 0;
            ImportButton.IsEnabled = true;
        }
        else
        {
            ClearUserList();
        }
    }

    private void ClearUserList()
    {
        UserComboBox.ItemsSource = null;
        ImportButton.IsEnabled = false;
    }

    /// <summary>
    ///     加载并预览存档信息
    /// </summary>
    public void LoadPack(string file)
    {
        _filePath = file;
        LoadingCard.IsVisible = true;
        PacksScrollViewer.IsVisible = false;
        PacksList.Children.Clear();

        Task.Run(() =>
        {
            // 创建临时目录用于解压预览
            var tempPath = Path.Combine(PathsList.TempPath, "TempWorld_" + Guid.NewGuid().ToString("N"));

            try
            {
                if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
                Directory.CreateDirectory(tempPath);

                ZipFile.ExtractToDirectory(file, tempPath);

                var worldData = ArchiveCheck.GetInfo(tempPath);
                Dispatcher.UIThread.Invoke(() =>
                {
                    // 添加 ArchiveItem 到预览列表
                    var item = new ArchiveItem(new ArchiveInfo
                    {
                        Name = worldData.Name,
                        Path = worldData.Path,
                        IconPath = File.Exists(worldData.IconPath) ? worldData.IconPath : "",
                        IsProject = worldData.IsProject,
                        LevelWorldData = worldData.LevelWorldData
                    }, true);
                    PacksList.Children.Add(item);

                    LoadingCard.IsVisible = false;
                    PacksScrollViewer.IsVisible = true;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    LoadingCard.BigTitle = "解析存档失败";
                    LoadingCard.Message = ex.Message;
                    ClearUserList();
                });
            }
        });
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_filePath) || UserComboBox.SelectedItem == null) return;

        var targetUser = (string)UserComboBox.SelectedItem;

        LoadingCard.BigTitle = "正在导入";
        LoadingCard.Message = $"正在导入至用户目录: {targetUser}...";
        LoadingCard.IsVisible = true;
        PacksScrollViewer.IsVisible = false;
        ImportButton.IsEnabled = false;
        ConfigPanel.IsEnabled = false;

        try
        {
            var selectedGame = _currentGames[InstanceComboBox.SelectedIndex];
            await Task.Run(() =>
            {
                var checker = new ArchiveCheck(selectedGame);
                // 调用后端逻辑执行正式解压到游戏目录
                checker.ImportWorldPack(_filePath, targetUser);
            });
            Close();
        }
        catch (Exception ex)
        {
            LoadingCard.BigTitle = "导入出错";
            LoadingCard.Message = ex.Message;
            ImportButton.IsEnabled = true;
            ConfigPanel.IsEnabled = true;
        }
    }
}