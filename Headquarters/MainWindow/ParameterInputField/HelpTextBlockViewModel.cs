using System.IO;

namespace Headquarters;

public class HelpTextBlockViewModel : ViewModelBase, IHelpTextBlockViewModel
{
    public HelpTextBlockViewModel(string help)
    {
        using var reader = new StringReader(help);
        HelpFirstLine = reader.ReadLine() ?? string.Empty;
        HelpDetail = reader.ReadToEnd() ?? string.Empty;
    }
    
    public string HelpFirstLine { get; }
    public string HelpDetail { get; }
}