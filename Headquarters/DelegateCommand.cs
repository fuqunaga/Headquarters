using System;
using System.Windows.Input;

namespace Headquarters;

public class DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    : ICommand
{
    private readonly Func<object?, bool> _canExecute = canExecute ?? (_ => true);


    public bool CanExecute(object? parameter) => _canExecute(parameter);

    public void Execute(object? parameter) => execute(parameter);
    
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}