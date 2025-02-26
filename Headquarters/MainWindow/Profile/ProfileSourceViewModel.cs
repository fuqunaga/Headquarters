namespace Headquarters;

public class ProfileSourceViewModel() : ViewModelBase, IHelpTextBlockViewModel
{
    private string _url = "";
    
    public string HelpFirstLine { get; set; } = "";
    public string HelpDetail { get; set; } = "";
    public string Url 
    { 
        get => _url;
        set => SetProperty(ref _url, value);
    }
    public bool IsReadOnly { get; set; }
}