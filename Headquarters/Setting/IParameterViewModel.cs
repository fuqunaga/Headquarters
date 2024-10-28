using System.ComponentModel;

namespace Headquarters;

public interface IParameterViewModel : INotifyPropertyChanged
{
    public string Name { get; }
    public bool IsEnabled { get; }
    public string Value { get; }
    public string ValueWhenDisabled { get; }
}