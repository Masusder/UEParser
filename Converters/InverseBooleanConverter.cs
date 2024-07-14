﻿using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace UEParser.Converters;

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool booleanValue)
        {
            return !booleanValue;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool booleanValue)
        {
            return !booleanValue;
        }
        return false;
    }
}