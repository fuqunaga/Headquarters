namespace Headquarters;

public enum OutputIcon
{
    None,
    Success,
    Failure,
}

public static class OutputIconEmoji
{
    public const string Success = "✅";
    public const string Failure = "⚠️";
    
    public static string GetEmoji(this OutputIcon icon)
    {
        return icon switch
        {
            OutputIcon.Success => Success,
            OutputIcon.Failure => Failure,
            _ => "",
        };
    }
}