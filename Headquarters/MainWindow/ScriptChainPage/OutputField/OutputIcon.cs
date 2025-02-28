using MaterialDesignThemes.Wpf;

namespace Headquarters;

public enum OutputIcon
{
    Information,
    NotStarted,
    Running,
    Success,
    Failure,
}

public static class OutputIconEmoji
{
    public const string Information = "ℹ️";
    public const string NotStarted = "⏳";
    public const string Running = "🔄";
    public const string Success = "✅";
    public const string Failure = "❌";
    
    public static string GetEmoji(this OutputIcon icon)
    {
        return icon switch
        {
            OutputIcon.Information => Information,
            OutputIcon.NotStarted => NotStarted,
            OutputIcon.Running => Running,
            OutputIcon.Success => Success,
            OutputIcon.Failure => Failure,
            _ => "",
        };
    }
}

public static class OutputIconDescription
{
    public static string GetDescription(this OutputIcon icon)
    {
        return icon switch
        {
            OutputIcon.Information => "スクリプト外のHeadquartersからの情報",
            OutputIcon.NotStarted => "実行前",
            OutputIcon.Running => "実行中",
            OutputIcon.Success => "正常終了",
            OutputIcon.Failure => "異常終了",
            _ => "",
        };
    }
}