namespace Headquarters;

public class ProfileSourceViewModel() : ViewModelBase, IHelpTextBlockViewModel
{
    public string HelpFirstLine { get; set; } = "";
    public string HelpDetail { get; set; } = "";
    public string Url { get; set; } = "";
    public bool IsReadOnly { get; set; }
}