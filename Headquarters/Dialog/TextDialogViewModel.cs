using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Headquarters;

public class TextDialogViewModel : DialogViewModelBase, INotifyDataErrorInfo
{
    public static readonly NotEmptyValidator NotEmptyValidator = new ("Field is required.");
    
    private string _text = "";
    private readonly Dictionary<string, string> _errors = new();
    private readonly List<Validator<string>> _validators = [NotEmptyValidator];
    
    public string Text 
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
            {
                ValidateText();
            }
        }
    }

    public IEnumerable<string> Suggestions { get; set; } = [];

    public TextDialogViewModel()
    {
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
        OnPropertyChanged(nameof(HasErrors));
    }

    private void AddError(string nameName, string nameIsRequired)
    {
        _errors[nameName] = nameIsRequired;
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameName));
        OnPropertyChanged(nameof(HasErrors));
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