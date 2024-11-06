using System.Collections.Generic;
using System.Linq;

namespace Headquarters;

public class OutputFieldViewModel : ViewModelBase
{
    private string _outputText = "";
    private int _completedCount;
    private int _failedCount;
    private bool _isCompletedVisible = true;
    private bool _isFailedVisible = true;
    
    private readonly List<IOutputUnit> _outputUnits = [];

    public string OutputText
    {
        get => _outputText;
        private set => SetProperty(ref _outputText, value);
    }

    public int CompletedCount
    {
        get => _completedCount;
        private set => SetProperty(ref _completedCount, value);
    }
    
    public int FailedCount
    {
        get => _failedCount;
        private set => SetProperty(ref _failedCount, value);
    }

    public bool IsCompletedVisible
    {
        get => _isCompletedVisible;
        set
        {
            var changed = SetProperty(ref _isCompletedVisible, value);
            if (changed) UpdateOutput();
        }

    }
    
    public bool IsFailedVisible
    {
        get => _isFailedVisible;
        set
        {
            var changed = SetProperty(ref _isFailedVisible, value);
            if (changed) UpdateOutput();
        }
    }

    
    public void AddOutputUnit(IOutputUnit outputUnit)
    {
        lock (_outputUnits)
        {
            _outputUnits.Add(outputUnit);
        }
    }
    
    public void AddScriptResult(ScriptResult result)
    {
        AddOutputUnit(new ScriptResultOutput(result));
    }
   
    public void UpdateOutput()
    {
        lock (_outputUnits)
        {
            OutputText = string.Join("\n", _outputUnits.Where(IsOutputUnitVisible).Select(OutputUnitToString));
            CompletedCount = _outputUnits.Count(unit => unit.Icon == OutputIcon.Completed);
            FailedCount = _outputUnits.Count(unit => unit.Icon == OutputIcon.Failed);
        }
    }
    
    private bool IsOutputUnitVisible(IOutputUnit unit)
    {
        return unit.Icon switch
        {
            OutputIcon.Completed => IsCompletedVisible,
            OutputIcon.Failed => IsFailedVisible,
            _ => true
        };
    }
    
    private static string OutputUnitToString(IOutputUnit unit)
    {
        var icon = unit.Icon switch
        {
            OutputIcon.Completed => "✅ ",
            OutputIcon.Failed => "❌ ",
            _ => ""
        };
        
        return $"{icon} {unit.Label}\n{unit.Text}";
    }
    
    public void Clear()
    {
        lock (_outputUnits)
        {
            _outputUnits.Clear();
        }

        OutputText = "";
    }
}