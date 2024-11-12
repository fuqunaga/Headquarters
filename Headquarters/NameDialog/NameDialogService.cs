using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace Headquarters;

public static class NameDialogService
{
    private static readonly NameDialog Dialog = new();
    private static readonly Binding Binding;
    private static readonly ValidationRule NotEmptyValidationRule = new NotEmptyValidationRule()
    {
        ValidatesOnTargetUpdated = true
    };
    
    
    static NameDialogService()
    {
        var binding = BindingOperations.GetBinding(Dialog.NameTextBox, TextBox.TextProperty);
        Binding = binding ?? throw new InvalidOperationException("Binding not found.");
    }

    public static async Task<(bool success, string)> ShowDialog(NameDialogViewModel viewModel, bool allowEmpty = false,
        params ValidationRule[] validationRules)
        => await ShowDialog(viewModel, allowEmpty ? validationRules : validationRules.Append(NotEmptyValidationRule));
    
    public static async Task<(bool success, string)> ShowDialog(NameDialogViewModel viewModel, params ValidationRule[] validationRules)
        => await ShowDialog(viewModel, validationRules.AsEnumerable());
    
    public static async Task<(bool success, string)> ShowDialog(NameDialogViewModel viewModel, IEnumerable<ValidationRule> validationRules)
    {
        Dialog.DataContext = viewModel;
        
        foreach(var validationRule in validationRules)
        {
            Binding.ValidationRules.Add(validationRule);
        }

        var result = await DialogHost.Show(Dialog, "RootDialog");

        Binding.ValidationRules.Clear();

        return (
            result != null && (bool)result,
            viewModel.Name ?? string.Empty
        );
    }
}