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