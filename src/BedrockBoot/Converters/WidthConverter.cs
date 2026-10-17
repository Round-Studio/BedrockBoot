using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace BedrockBoot.Converters;

public class WidthConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is double width && !double.IsNaN(width))
        {
            return width + 32;
        }
        return 114;
    }
}