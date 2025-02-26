using System;
using System.Globalization;
using System.Windows.Data;

#nullable disable

namespace Headquarters;

public class EnumToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Enum)
        {
            return System.Convert.ToInt32(value);
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int && targetType.IsEnum)
        {
            return Enum.ToObject(targetType, value);
        }

        return null;
    }
}