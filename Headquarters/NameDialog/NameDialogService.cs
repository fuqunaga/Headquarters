using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace Headquarters;

/// <summary>
/// NameDialogを表示するサービス
///
/// TextBoxとComboBoxのバインディングを１つにまとめたいがいい方法がわからず２重に管理している
/// </summary>
public static class NameDialogService
{
    private static readonly NameDialog Dialog = new();
    private static readonly Binding TextBoxBinding;
    private static readonly Binding ComboBoxBinding;
    private static readonly ValidationRule NotEmptyValidationRule = new NotEmptyValidationRule()
    {
        ValidatesOnTargetUpdated = true
    };
    
    
    static NameDialogService()
    {
        TextBoxBinding = BindingOperations.GetBinding(Dialog.NameTextBox, TextBox.TextProperty) 
                         ?? throw new InvalidOperationException("Binding not found.");
        
        ComboBoxBinding = BindingOperations.GetBinding(Dialog.NameComboBox, ComboBox.TextProperty)
                          ?? throw new InvalidOperationException("Binding not found.");
    }

    public static async Task<(bool success, string)> ShowDialog(NameDialogViewModel viewModel, params ValidationRule[] validationRules)
        => await ShowDialog(viewModel, false, validationRules);

    public static async Task<(bool success, string)> ShowDialog(NameDialogViewModel viewModel, bool allowEmpty = false,
        params ValidationRule[] validationRules)
        => await ShowDialog(viewModel, allowEmpty ? validationRules : validationRules.Append(NotEmptyValidationRule));

    private static async Task<(bool success, string)> ShowDialog(NameDialogViewModel viewModel, IEnumerable<ValidationRule> validationRules)
    {
        var targetBinding = viewModel.Suggestions is not null ? ComboBoxBinding : TextBoxBinding;
        
        foreach(var validationRule in validationRules)
        {
            targetBinding.ValidationRules.Add(validationRule);
        }
        
        Dialog.DataContext = viewModel;
        var result = await DialogHost.Show(Dialog, "RootDialog");

        targetBinding.ValidationRules.Clear();

        return (
            result != null && (bool)result,
            viewModel.Name ?? string.Empty
        );
    }
}