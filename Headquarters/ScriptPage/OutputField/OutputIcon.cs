namespace Headquarters;

public enum OutputIcon
{
    None,
    Completed,
    Failed,
}

public static class OutputIconEmoji
{
    public const string Completed = "✅";
    public const string Failed = "⚠️";
    
    public static string GetEmoji(this OutputIcon icon)
    {
        return icon switch
        {
            OutputIcon.Completed => Completed,
            OutputIcon.Failed => Failed,
            _ => "",
        };
    }
}