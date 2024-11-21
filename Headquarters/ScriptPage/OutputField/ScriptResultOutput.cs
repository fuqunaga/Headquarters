using System;

namespace Headquarters;

public class ScriptResultOutput: IOutputUnit
{
    public event Action? onPropertyChanged;
    private readonly ScriptResult _result;

    public OutputIcon Icon =>
        _result.Result == null
            ? _result.Info == null
                ? OutputIcon.NotStarted
                : OutputIcon.Running
            : _result.Result.IsSucceed
                ? OutputIcon.Success
                : OutputIcon.Failure;

    public string Label => _result.Label;
    public string Text => _result.GetResultString();
    

    public ScriptResultOutput(ScriptResult result)
    {
        _result = result;
        _result.onPropertyChanged += () => onPropertyChanged?.Invoke();
    }
}
