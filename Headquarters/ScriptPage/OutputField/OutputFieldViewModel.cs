using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Headquarters;

public class OutputFieldViewModel : ViewModelBase
{
    public ObservableCollection<OutputFilterButtonViewModel> FilterButtonViewModels { get; } = new(
        Enum.GetValues(typeof(OutputIcon))
            .Cast<OutputIcon>()
            .Select(icon => new OutputFilterButtonViewModel(icon))
    );
    
    public ObservableCollection<OutputUnitViewModel> OutputUnits { get; } = [];
    
    public void AddOutputUnit(IOutputUnit outputUnit)
    {
        lock (OutputUnits)
        {
            OutputUnits.Add(new OutputUnitViewModel(outputUnit));
        }
    }
    
    public void AddScriptResult(ScriptResult result)
    {
        AddOutputUnit(new ScriptResultOutput(result));
        result.onPropertyChanged += UpdateOutput;
    }

    private void UpdateOutput()
    {
        lock (OutputUnits)
        {
            var buttonAndNewCounts = OutputUnits
                .GroupBy(unit => unit.Icon)
                .Select(group => (icon: group.Key, count: group.Count()))
                .Join(FilterButtonViewModels,
                    countData => countData.icon,
                    button => button.Icon,
                    (countData, button) => (button, countData.count)
                );
            
            foreach (var (button, newCount) in buttonAndNewCounts)
            {
                button.Count = newCount;
            }
        }
    }
    
    public void Clear()
    {
        lock (OutputUnits)
        {
            OutputUnits.Clear();
        }

        UpdateOutput();
    }
}