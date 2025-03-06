using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Headquarters;

public class TextBoxDialogViewModel : LabelDialogViewModel, INotifyDataErrorInfo
{
    public static readonly NotEmptyValidator NotEmptyValidator = new ("Field is required.");
    
    private readonly Dictionary<string, string> _errors = new();
    private readonly List<Validator<string>> _validators = [NotEmptyValidator];

    public override bool IsOkButtonEnabled => !HasErrors;

    public TextBoxDialogViewModel()
    {
        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Text))
            {
                ValidateText();
            }
        };
        
        ValidateText();
    }

    public void AddValidator(Validator<string> validator)
    {
        _validators.Add(validator);
        ValidateText();
    }
    
    private void ValidateText()
    {
        var invalidMessage = _validators
            .FirstOrDefault(validator => !validator.Validate(Text))?
            .InvalidMessage;
        
        if (invalidMessage != null)
        {
            AddError(nameof(Text), invalidMessage);
        }
        else
        {
            RemoveError(nameof(Text));    
        }
    }

    private void RemoveError(string propertyName)
    {
        _errors.Remove(propertyName);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(IsOkButtonEnabled));
    }

    private void AddError(string nameName, string nameIsRequired)
    {
        _errors[nameName] = nameIsRequired;
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameName));
        OnPropertyChanged(nameof(IsOkButtonEnabled));
    }


    #region INotifyDataErrorInfo

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    
    public bool HasErrors => _errors.Count > 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (propertyName is not null
            && _errors.TryGetValue(propertyName, out var error))
        {
            return new[] { error };
        }
        
        return Array.Empty<string>();
    }
    
    #endregion
}