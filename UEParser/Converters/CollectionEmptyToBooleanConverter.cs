using Avalonia.Data.Converters;
using System;
using System.Collections;
using System.Globalization;

namespace UEParser.Converters;

public class CollectionEmptyToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ICollection collection && collection.Count > 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}