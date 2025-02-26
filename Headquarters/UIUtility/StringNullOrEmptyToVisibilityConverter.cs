using System;
using System.Globalization;
using System.Windows.Data;

namespace Headquarters;

public class StringNullOrEmptyToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return string.IsNullOrEmpty(str) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        return System.Windows.Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}