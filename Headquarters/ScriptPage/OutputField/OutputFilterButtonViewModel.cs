namespace Headquarters;

public class OutputFilterButtonViewModel(OutputIcon icon) : ViewModelBase
{
    private int _count;
    private bool _isOutputVisible = true;

    public OutputIcon Icon => icon;

    public string IconEmoji => icon.GetEmoji();
    
    public string IconDescription => icon.GetDescription();

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    public bool IsOutputVisible
    {
        get => _isOutputVisible;
        set => SetProperty(ref _isOutputVisible, value);
    }
    
}