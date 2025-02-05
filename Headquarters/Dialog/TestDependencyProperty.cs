using System.Windows;

namespace Headquarters;

public static class TestDependencyProperty
{
    public static readonly DependencyProperty NameProperty = DependencyProperty.RegisterAttached("Name", typeof (string), typeof (TestDependencyProperty), (PropertyMetadata) new FrameworkPropertyMetadata((object) "", FrameworkPropertyMetadataOptions.Inherits));
    
    public static string GetName(DependencyObject element)
    {
        return (string) element.GetValue(NameProperty);
    }

    public static void SetName(DependencyObject element, string value)
    {
        element.SetValue(NameProperty, (object) value);
    }
}