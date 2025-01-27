namespace Headquarters;

public class ScriptChainHeaderViewModel : ViewModelBase
{
    private bool _isActive;
    private bool _isSelected;

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public ScriptPageViewModel ScriptPageViewModel { get; }

    public ScriptChainHeaderViewModel(ScriptPageViewModel scriptPageViewModel)
    {
        ScriptPageViewModel = scriptPageViewModel;
    }
}