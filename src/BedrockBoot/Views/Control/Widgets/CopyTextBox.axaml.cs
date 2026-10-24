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
using Avalonia.Data;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Control.Widgets;

public partial class CopyTextBox : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<CopyTextBox, string>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<CopyTextBox, bool>(nameof(IsReadOnly));

    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<CopyTextBox, string>(nameof(Watermark));

    public CopyTextBox()
    {
        InitializeComponent();

        // 初始化时同步属性
        UpdateTextBoxProperties();

        // 监听控件属性变化
        PropertyChanged += OnPropertyChanged;
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (MainTextBox == null) return;

        if (e.Property == TextProperty)
        {
            var newValue = e.NewValue as string;
            if (MainTextBox.Text != newValue) MainTextBox.Text = newValue;
        }
        else if (e.Property == IsReadOnlyProperty)
        {
            MainTextBox.IsReadOnly = e.NewValue is true;
        }
        else if (e.Property == WatermarkProperty)
        {
            MainTextBox.Watermark = e.NewValue as string;
        }
    }

    private void UpdateTextBoxProperties()
    {
        if (MainTextBox == null) return;

        MainTextBox.Text = Text;
        MainTextBox.IsReadOnly = IsReadOnly;
        MainTextBox.Watermark = Watermark;
    }

    private void MainTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (MainTextBox != null && Text != MainTextBox.Text) Text = MainTextBox.Text;
    }

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Text)) return;

        // 获取剪贴板
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(Text);
            await ShowCopyFeedback();
        }
    }

    private async Task ShowCopyFeedback()
    {
        if (CopyButton == null) return;

        var originalContent = CopyButton.Content;

        // 临时显示成功图标
        CopyButton.Content = new FontIcon
        {
            Glyph = "\uE73E"
        };

        await Task.Delay(1000);

        // 恢复原始图标
        CopyButton.Content = originalContent;
    }
}