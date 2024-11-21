namespace Headquarters;

public enum OutputIcon
{
    NotStarted,
    Running,
    Success,
    Failure,
}

public static class OutputIconEmoji
{
    public const string NotStarted = "⏳";
    public const string Running = "🔄";
    public const string Success = "✅";
    public const string Failure = "❌";
    
    public static string GetEmoji(this OutputIcon icon)
    {
        return icon switch
        {
            OutputIcon.NotStarted => NotStarted,
            OutputIcon.Running => Running,
            OutputIcon.Success => Success,
            OutputIcon.Failure => Failure,
            _ => "",
        };
    }
}