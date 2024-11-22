using System.Windows;
using MaterialDesignThemes.Wpf;

namespace Headquarters;

public static class DataGridAssist
{
    public static readonly DependencyProperty IsUnlockedProperty = DependencyProperty.RegisterAttached("IsUnlocked", typeof (bool), typeof (DataGridAssist), new PropertyMetadata((object) true));
    
    public static void SetIsUnlocked(DependencyObject element, bool value)
    {
        element.SetValue(IsUnlockedProperty, (object) value);
    }

    public static bool GetIsUnlocked(DependencyObject element)
    {
        return (bool) element.GetValue(IsUnlockedProperty);
    }
}