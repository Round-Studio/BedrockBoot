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
using System.Globalization;
using Avalonia.Data.Converters;

namespace BedrockBoot.Converters;

public class TaskPanelWidthToMarginConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var top = 22;
        var right = 8;
        if (values.Count > 0 && values[0] is double width && !double.IsNaN(width))
        {
            return new Avalonia.Thickness(0, top, 22 + right + width, 0);
        }
        return new Avalonia.Thickness(0, top, 22 + right, 0);
    }
}
public class AccountPanelWidthToMarginConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var top = 62;
        var right = 8;
        if (values.Count > 0 && values[0] is double width && !double.IsNaN(width))
        {
            return new Avalonia.Thickness(0, top, 22 + right + width, 0);
        }
        return new Avalonia.Thickness(0, top, 22 + right, 0);
    }
}