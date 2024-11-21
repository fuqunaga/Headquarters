namespace Headquarters;

public class OutputUnitViewModel : ViewModelBase
{
    private readonly IOutputUnit _outputUnit;
    private OutputIcon _icon = OutputIcon.NotStarted;
    private string _iconEmoji = OutputIconEmoji.NotStarted;
    private string _text = "";

    public OutputIcon Icon
    {
        get => _icon;
        private set
        {
            if ( SetProperty(ref _icon, value) )
            {
                IconEmoji = value.GetEmoji();
            }
        }
    }

    public string IconEmoji
    {
        get => _iconEmoji;
        private set => SetProperty(ref _iconEmoji, value);
    }
    public string Text
    {
        get => _text;
        private set => SetProperty(ref _text, value);
    }
    
    public OutputUnitViewModel(IOutputUnit outputUnit)
    {
        _outputUnit = outputUnit;
        if (_outputUnit is ScriptResultOutput scriptResultOutput)
        {
            scriptResultOutput.onPropertyChanged += UpdateProperties;
        }
        
        UpdateProperties();
    }
    
    private void UpdateProperties()
    {
        Icon = _outputUnit.Icon;
        
        var unitText = _outputUnit.Text;
        Text = string.IsNullOrEmpty(unitText)
            ? _outputUnit.Label
            : $"{_outputUnit.Label}\n{_outputUnit.Text}";
    }
}