using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Headquarters;

public class SettingPageViewModel : ViewModelBase
{
    public event Action? closeRequested;
    
    private ParameterSet? _parameterSet;
    
    
    public ObservableCollection<ParameterSetViewModel> ParameterSetViewModels { get; } = [];
    
    public ICommand CloseCommand { get; }
    
    
    
    public SettingPageViewModel()
    {
        CloseCommand = new DelegateCommand(_ => closeRequested?.Invoke());
    }

    public void SetParameterSet(ParameterSet parameterSet)
    {
        _parameterSet = parameterSet;
        foreach (var parameterSetViewModel in GlobalParameter.CreateParameterSetViewModels(parameterSet))
        {
            ParameterSetViewModels.Add(parameterSetViewModel);
        }
    }

    public ParameterSet? GetParameterSet() => _parameterSet;
}