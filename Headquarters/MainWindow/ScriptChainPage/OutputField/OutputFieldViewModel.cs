using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Headquarters;

public class OutputFieldViewModel : ViewModelBase
{
    private string _searchText = "";
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnSearchTextChanged();
            }
        }
    }
    
    public ObservableCollection<OutputFilterButtonViewModel> FilterButtonViewModels { get; } = new(
        Enum.GetValues(typeof(OutputIcon))
            .Cast<OutputIcon>()
            .Select(icon => new OutputFilterButtonViewModel(icon))
    );
    
    public CollectionViewSource OutputUnitsViewSource { get; } = new();
    private ObservableCollection<OutputUnitViewModel> OutputUnits { get; } = [];
    
    public OutputFieldViewModel()
    {
        OutputUnits.CollectionChanged += (_, _) => UpdateFilterButtonCount();

        foreach (var button in FilterButtonViewModels)
        {
            button.PropertyChanged += (_, arg) =>
            {
                if (arg.PropertyName == nameof(OutputFilterButtonViewModel.IsOutputVisible))
                {
                    OutputUnitsViewSource.View.Refresh();
                }
            };
        }

        OutputUnitsViewSource.Source = OutputUnits;
        
        OutputUnitsViewSource.IsLiveFilteringRequested = true;
        OutputUnitsViewSource.LiveFilteringProperties.Add(nameof(OutputUnitViewModel.Icon));
        OutputUnitsViewSource.Filter += (_, e) =>
        {
            if (e.Item is not OutputUnitViewModel outputUnitViewModel) return;
            
            e.Accepted = GetFilterButtonViewModel(outputUnitViewModel.Icon).IsOutputVisible
                         && (
                             string.IsNullOrWhiteSpace(SearchText)
                             || (outputUnitViewModel.Text.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                         );
        };
    }
        
    private void OnSearchTextChanged()
    {
        const string propertyName = nameof(OutputUnitViewModel.Text);
        
        var filteringProperties = OutputUnitsViewSource.LiveFilteringProperties;
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            filteringProperties.Remove(propertyName);
        }
        else if(filteringProperties.Contains(propertyName))
        {
            filteringProperties.Add(propertyName);
        }
        
        OutputUnitsViewSource.View.Refresh();
    }
    
    public void AddOutputUnit(IOutputUnit outputUnit)
    {
        var outputUnitViewModel = new OutputUnitViewModel(outputUnit);
        outputUnitViewModel.PropertyChanged += (_, arg) =>
        {
            if (arg.PropertyName == nameof(OutputUnitViewModel.Icon))
            {
                // 別スレッドから呼ばれる可能性があるのでメインスレッドで実行
                Application.Current.Dispatcher.Invoke(UpdateFilterButtonCount);
            }
        };

        OutputUnits.Add(outputUnitViewModel);
    }
    
    public void AddScriptResult(ScriptExecutionInfo executionInfo) => AddOutputUnit(new ScriptResultOutput(executionInfo));

    private OutputFilterButtonViewModel GetFilterButtonViewModel(OutputIcon icon)
        => FilterButtonViewModels.First(button => button.Icon == icon);
    
    private void UpdateFilterButtonCount()
    {
        foreach(var button in FilterButtonViewModels)
        {
            button.Count = OutputUnits.Count(u => u.Icon == button.Icon);
        }
    }
    
    public void Clear() => OutputUnits.Clear();
}