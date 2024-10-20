using Avalonia.Media;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace UEParser.Converters;

public class FileExtensionToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string fileExtension)
        {
            return fileExtension switch
            {
                "pak" => Brushes.OrangeRed,
                "ushaderbytecodeindex" => Brushes.YellowGreen,
                "ushaderbytecode" => new SolidColorBrush(Color.Parse("#2f4a7a")),
                "index" => Brushes.Yellow,
                "liteinfo" => Brushes.DodgerBlue,
                _ => Brushes.White
            };
        }

        return Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
