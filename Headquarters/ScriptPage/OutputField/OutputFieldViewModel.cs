using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Headquarters;

public class OutputFieldViewModel : ViewModelBase
{
    public ObservableCollection<OutputFilterButtonViewModel> FilterButtonViewModels { get; } = new(
        Enum.GetValues(typeof(OutputIcon))
            .Cast<OutputIcon>()
            .Select(icon => new OutputFilterButtonViewModel(icon))
    );
    
    public ObservableCollection<OutputUnitViewModel> OutputUnits { get; } = [];
    
    public OutputFieldViewModel()
    {
        OutputUnits.CollectionChanged += (_, _) => UpdateFilterButtonCount();
        
        foreach(var button in FilterButtonViewModels)
        {
            button.PropertyChanged += (_, arg) =>
            {
                if (arg.PropertyName == nameof(OutputFilterButtonViewModel.IsOutputVisible))
                {
                    UpdateOutputUnitsVisible(button.Icon, button.IsOutputVisible);
                }
            };
        }
    }
    
    public void AddOutputUnit(IOutputUnit outputUnit)
    {
        var outputUnitViewModel = new OutputUnitViewModel(outputUnit);
        outputUnitViewModel.PropertyChanged += (_, arg) =>
        {
            if (arg.PropertyName == nameof(OutputUnitViewModel.Icon))
            {
                // 別スレッドから呼ばれる可能性があるのでメインスレッドで実行
                Application.Current.Dispatcher.Invoke(() => OnOutputUnityViewModelIconChanged(outputUnitViewModel));
            }
        };

        OutputUnits.Add(outputUnitViewModel);
    }
    
    public void AddScriptResult(ScriptResult result) => AddOutputUnit(new ScriptResultOutput(result));

    private OutputFilterButtonViewModel GetFilterButtonViewModel(OutputIcon icon)
        => FilterButtonViewModels.First(button => button.Icon == icon);
    
    private void OnOutputUnityViewModelIconChanged(OutputUnitViewModel outputUnitViewModel)
    {
        outputUnitViewModel.IsVisible = GetFilterButtonViewModel(outputUnitViewModel.Icon).IsOutputVisible;
        UpdateFilterButtonCount();
    }
    
    private void UpdateFilterButtonCount()
    {
        foreach(var button in FilterButtonViewModels)
        {
            button.Count = OutputUnits.Count(u => u.Icon == button.Icon);
        }
    }
    
    private void UpdateOutputUnitsVisible(OutputIcon icon, bool isVisible)
    {
        foreach (var unit in OutputUnits.Where(u => u.Icon == icon))
        {
            unit.IsVisible = isVisible;
        }
    }
    
    public void Clear() => OutputUnits.Clear();
}