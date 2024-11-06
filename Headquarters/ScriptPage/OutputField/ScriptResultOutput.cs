namespace Headquarters;

public readonly struct ScriptResultOutput(ScriptResult result) : IOutputUnit
{
    public OutputIcon Icon => result.Result?.IsSucceed switch
    {
        true => OutputIcon.Completed,
        false => OutputIcon.Failed,
        _ => OutputIcon.None
    };

    public string Label => result.Label;
    public string Text => result.GetResultString();
}
