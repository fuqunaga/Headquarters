namespace Headquarters;

public readonly struct ScriptResultOutput(ScriptResult result) : IOutputUnit
{
    public OutputIcon Icon => result.Result?.IsSucceed switch
    {
        true => OutputIcon.Success,
        false => OutputIcon.Failure,
        _ => OutputIcon.None
    };

    public string Label => result.Label;
    public string Text => result.GetResultString();
}
