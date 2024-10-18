namespace Headquarters;

public class ParameterViewModel(string name, ParameterSet parameterSet) : ViewModelBase, IParameterViewModel
{
    public string Name => name;
    public bool IsEnabled  => true;

    public string Value
    {
        get => parameterSet.Get(Name);
        set
        {
            if (parameterSet.Set(Name, value))
            {
                OnPropertyChanged();
            }
        }
    }
    public string ValueWhenDisabled  => Value;
}