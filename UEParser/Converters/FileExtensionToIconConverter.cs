using System;
using System.Globalization;
using System.Collections.Generic;
using Avalonia.Data.Converters;

namespace UEParser.Converters;

public class FileExtensionToIconConverter : IValueConverter
{
    private readonly Dictionary<string, string> IconMapping = new()
    {
        { "pak", "fa-solid fa-box" },
        { "ushaderbytecodeindex", "fa-solid fa-tag" },
        { "ushaderbytecode", "fa-brands fa-unity"},
        { "index", "fa-solid fa-tag" },
        { "liteinfo", "fa-solid fa-circle-nodes" }
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string extension)
        {
            if (IconMapping.TryGetValue(extension, out var iconPath))
            {
                return iconPath;
            }
        }

        return "fa-solid fa-circle-question";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
