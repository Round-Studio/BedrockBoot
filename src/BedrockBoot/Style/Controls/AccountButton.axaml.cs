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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using BedrockBoot.Views.Control.Widgets;

namespace BedrockBoot.Style.Controls;

public class AccountButton : Button
{
    private LocalImageRenderWidget? _headerImage;
    private TextBlock? _accountName;

    public static readonly StyledProperty<string?> HeaderImageUrlProperty =
        AvaloniaProperty.Register<AccountButton, string?>(nameof(HeaderImageUrl));

    public string? HeaderImageUrl
    {
        get => GetValue(HeaderImageUrlProperty);
        set => SetValue(HeaderImageUrlProperty, value);
    }

    public static readonly StyledProperty<string?> AccountNameProperty =
        AvaloniaProperty.Register<AccountButton, string?>(nameof(AccountName));

    public string? AccountName
    {
        get => GetValue(AccountNameProperty);
        set => SetValue(AccountNameProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _headerImage = e.NameScope.Find<LocalImageRenderWidget>("PART_HeaderImage");
        _accountName = e.NameScope.Find<TextBlock>("PART_AccountName");

        UpdateHeaderImage();
        UpdateAccountName();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HeaderImageUrlProperty)
        {
            UpdateHeaderImage();
        }
        else if (change.Property == AccountNameProperty)
        {
            UpdateAccountName();
        }
    }

    private void UpdateHeaderImage()
    {
        if (_headerImage != null && !string.IsNullOrEmpty(HeaderImageUrl))
        {
            _headerImage.ImageUrl = HeaderImageUrl;
        }
    }

    private void UpdateAccountName()
    {
        if (_accountName != null)
        {
            _accountName.Text = AccountName ?? "未登录";
        }
    }
}