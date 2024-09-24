using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace UEParser.Converters;

public class RegistersVisibilityConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2)
            return false;

        int count = 0;
        bool isFetching = false;

        if (values[0] != AvaloniaProperty.UnsetValue)
        {
            if (values[0] is int intCount)
            {
                count = intCount;
            }
        }

        if (values[1] != AvaloniaProperty.UnsetValue)
        {
            if (values[1] is bool boolFetching)
            {
                isFetching = boolFetching;
            }
        }

        return count == 0 && !isFetching;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}