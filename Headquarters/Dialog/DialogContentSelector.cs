using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Headquarters;

public class DialogContentSelector : DataTemplateSelector
{
    public DataTemplate? TextBoxTemplate { get; set; }
    public DataTemplate? ComboBoxTemplate { get; set; }
    public DataTemplate? LabelTemplate { get; set; }
    
    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        // Null value can be passed by IDE designer
        if (item is not TextDialogViewModel viewModel) return null;

        if (!viewModel.IsEditable)
        {
            return LabelTemplate;
        }

        return viewModel.Suggestions.Any()
            ? ComboBoxTemplate
            : TextBoxTemplate;
    }
}