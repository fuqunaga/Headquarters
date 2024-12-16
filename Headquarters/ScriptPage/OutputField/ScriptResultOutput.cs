using System;

namespace Headquarters;

public class ScriptResultOutput: IOutputUnit
{
    public event Action? onPropertyChanged;
    private readonly ScriptExecutionInfo _executionInfo;

    public OutputIcon Icon =>
        _executionInfo.Result == null
            ? _executionInfo.Info == null
                ? OutputIcon.NotStarted
                : OutputIcon.Running
            : _executionInfo.Result.IsSucceed
                ? OutputIcon.Success
                : OutputIcon.Failure;

    public string Label => _executionInfo.Label;
    public string Text => _executionInfo.GetResultString();
    

    public ScriptResultOutput(ScriptExecutionInfo executionInfo)
    {
        _executionInfo = executionInfo;
        _executionInfo.onPropertyChanged += () => onPropertyChanged?.Invoke();
    }
}
