using System;
using System.Windows;
using System.Windows.Controls;

namespace Headquarters
{
    public class BoolDataTemplateSelector : DataTemplateSelector
    {
        public string BoolPropertyName { get; set; }

        public DataTemplate TrueTemplate { get; set; }
        public DataTemplate FalseTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // Null value can be passed by IDE designer
            if (item == null) return null;

            var b = string.IsNullOrEmpty(BoolPropertyName)
                ? Convert.ToBoolean(item)
                : (bool)item.GetType().GetProperty(BoolPropertyName).GetValue(item);
            
            return b ? TrueTemplate : FalseTemplate;
        }
    }
}
