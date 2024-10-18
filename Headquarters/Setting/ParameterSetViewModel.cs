using System.Collections.ObjectModel;

namespace Headquarters;

public class ParameterSetViewModel(string description = "")
{
    public string Description { get; } = description;
    public ObservableCollection<IParameterViewModel> Parameters { get; } = [];
}