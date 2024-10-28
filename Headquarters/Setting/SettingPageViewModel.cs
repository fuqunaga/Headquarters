using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Headquarters;

public class SettingPageViewModel : ViewModelBase
{
    public event Action? closeRequested;
    
    public ObservableCollection<ParameterSetViewModel> ParameterSetViewModels { get; } = [];
    
    public ICommand CloseCommand { get; }
    
    
    
    public SettingPageViewModel()
    {
        CloseCommand = new DelegateCommand(_ => closeRequested?.Invoke());
    }

    public void AddGlobalParameterViewModel()
    {
        foreach (var parameterSetViewModel in GlobalParameter.CreateParameterSetViewModels())
        {
            ParameterSetViewModels.Add(parameterSetViewModel);
        }
    }
}