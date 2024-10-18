using System;
using System.Windows.Input;

namespace Headquarters;

public class SettingPageViewModel : ViewModelBase
{
    public event Action? closeRequested;
    
    public ICommand CloseCommand { get; }
    
    
    public SettingPageViewModel()
    {
        CloseCommand = new DelegateCommand(_ => closeRequested?.Invoke());
    }
}