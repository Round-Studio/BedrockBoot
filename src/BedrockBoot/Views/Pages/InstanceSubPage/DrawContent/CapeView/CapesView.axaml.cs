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
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.CapeView;

public partial class CapesView : UserControl
{
    public CapesView(VersionConfig versionInfo)
    {
        VersionInfo = versionInfo;
        InitializeComponent();
        UpdateContent();
    }

    public VersionConfig VersionInfo { get; set; }

    public Action<CapesPackInfo> EditAction { get; set; }

    private void UpdateContent()
    {
        SavesBox.Children.Clear();

        var packs = CapesPackManager.LoadPacks(VersionInfo.VersionPath);
        NullBox.IsVisible = packs.Count == 0;

        foreach (var pack in packs)
        {
            SavesBox.Children.Add(BuildPackExpander(pack));
        }
    }

    private SettingExpander BuildPackExpander(CapesPackInfo pack)
    {
        var expander = new SettingExpander
        {
            Header = pack.PackName,
        };

        if (!string.IsNullOrEmpty(pack.IconPath))
        {
            try
            {
                expander.ImageIcon = new Bitmap(pack.IconPath);
                expander.IsFontIcon = false;
            }
            catch
            {
                expander.Glyph = "\uE7B8";
            }
        }
        else
        {
            expander.Glyph = "\uE7B8";
        }

        var description = pack.CapeKeys.Count > 0
            ? $"{pack.CapeKeys.Count} 件披风"
            : "空披风包";
        if (!string.IsNullOrWhiteSpace(pack.Authors))
        {
            description += $" · {"作者"} {pack.Authors}";
        }
        expander.Description = description;

        var openCard = new SettingCard
        {
            Header = "包设置",
            Description = "编辑包名、作者、图标并管理披风文件",
            Glyph = "\uE713",
            IsClickable = true,
        };
        openCard.Click += (_, _) => EditAction?.Invoke(pack);

        var deleteCard = new SettingCard
        {
            Header = "删除披风包",
            Description = "删除此披风包及其全部内容",
            Glyph = "\uE74D",
            IsClickable = true,
        };
        deleteCard.Click += (_, _) =>
        {
            DialogHost.Show(new DialogInfo
            {
                Title = "删除披风包",
                Content = string.Format("确定要删除披风包 “{0}” 吗？此操作不可撤销。", pack.PackName),
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                AccountButton = DialogButtons.PrimaryButton,
                PrimaryAction = () =>
                {
                    CapesPackManager.DeletePack(VersionInfo.VersionPath, pack.FolderName);
                    Dispatcher.UIThread.Invoke(UpdateContent);
                }
            });
        };

        var panel = new StackPanel();
        panel.Children.Add(openCard);
        panel.Children.Add(deleteCard);
        expander.Content = panel;

        return expander;
    }

    private async void CreateGroupBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var nameBox = new TextBox { Watermark = "披风包名称" };
        var authorBox = new TextBox { Watermark = "作者" };
        var iconText = new TextBlock { Opacity = 0.6, Text = "未选择图标" };
        byte[]? iconBytes = null;

        var pickIcon = new Button
        {
            Content = "选择图标 (pack_icon.png)",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
        };
        pickIcon.Click += async (_, _) =>
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择披风包图标",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } } }
            });
            if (files is { Count: >= 1 })
            {
                iconBytes = await File.ReadAllBytesAsync(files[0].Path.LocalPath);
                iconText.Text = files[0].Name;
            }
        };

        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(nameBox);
        panel.Children.Add(authorBox);
        panel.Children.Add(pickIcon);
        panel.Children.Add(iconText);

        DialogHost.Show(new DialogInfo
        {
            Title = "创建披风群组",
            Content = panel,
            PrimaryButtonText = "创建",
            CloseButtonText = "取消",
            AccountButton = DialogButtons.PrimaryButton,
            PrimaryAction = () =>
            {
                var packName = nameBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(packName)) return;

                CapesPackManager.CreatePack(VersionInfo.VersionPath, packName,
                    authorBox.Text?.Trim() ?? string.Empty, iconBytes);
                Dispatcher.UIThread.Invoke(UpdateContent);
            }
        });
    }
}
