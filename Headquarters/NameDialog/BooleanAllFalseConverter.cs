using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Headquarters;

public class BooleanAllFalseConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        => values.OfType<bool>().All(b => !b);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
