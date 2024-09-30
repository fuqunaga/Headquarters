using System;
using System.Windows.Input;

namespace Headquarters;

public class DelegateCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool> _canExecute;

    
    public DelegateCommand(Action<object?>  execute) : this(execute, null)
    {
    }

    public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute)
    {
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecute = canExecute ?? (_ => true);
    }
    
    public bool CanExecute(object? parameter) => _canExecute(parameter);

    public void Execute(object? parameter) => _execute(parameter);
    
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}