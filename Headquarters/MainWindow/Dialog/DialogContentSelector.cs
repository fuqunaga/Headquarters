using System.Windows;
using System.Windows.Controls;

namespace Headquarters;

public class DialogContentSelector : DataTemplateSelector
{
    public DataTemplate? TextBoxTemplate { get; set; }
    public DataTemplate? ComboBoxTemplate { get; set; }
    public DataTemplate? LabelTemplate { get; set; }
    public DataTemplate? ListTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        return item switch
        {
            ComboBoxDialogViewModel => ComboBoxTemplate,
            TextBoxDialogViewModel => TextBoxTemplate,
            ListDialogViewModel => ListTemplate,
            LabelDialogViewModel => LabelTemplate,
            _ => null
        };
    }
}