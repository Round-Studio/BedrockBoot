using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace BedrockBoot.Converters;

public class WidthToRightTopOriginConverter : IValueConverter
{
    public static readonly WidthToRightTopOriginConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width && width > 0)
            return new RelativePoint(width, 0, RelativeUnit.Absolute);

        return new RelativePoint(1, 0, RelativeUnit.Relative);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}