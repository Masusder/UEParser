using Avalonia.Data.Converters;
using System;
using System.Globalization;
using UEParser.Utils;

namespace UEParser.Converters;

public class SizeToBytesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long size)
        {
            return StringUtils.FormatBytes(size);
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}