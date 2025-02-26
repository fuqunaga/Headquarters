namespace Headquarters;

public class LabelDialogViewModel : DialogViewModelBase
{
    private string _text = "";
    
    public string Text 
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }
}