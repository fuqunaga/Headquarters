namespace Headquarters;

public class ScriptChainHeaderViewModel : ViewModelBase
{
    private bool _isSelected;


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