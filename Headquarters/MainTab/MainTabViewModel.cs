using System;
using System.Windows.Input;

namespace Headquarters;

public class MainTabViewModel : ViewModelBase
{
    public static event Action<MainTabViewModel>? newTabEvent;
    public static event Action<MainTabViewModel>? duplicateTabEvent;
    
    public static Func<MainTabViewModel> Factory => () => new MainTabViewModel();

    private readonly TabParameterSet _tabParameterSet;
    private string _header;
    private bool _isLocked;

    public ICommand NewTabCommand { get; }
    public ICommand DuplicateTabCommand { get; }
    public ICommand ToggleLockCommand { get; private set; }

    public string Header
    {
        get => _header;
        set => SetProperty(ref _header, value);
    }
    
    public bool IsLocked {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }
    
    
    public IpListViewModel IpListViewModel { get; } = new();
    public ScriptPageViewModel ScriptPageViewModel { get; } = new();

    
    public MainTabViewModel(MainTabData data)
    {
        _header = data.TabHeader;
        IsLocked = data.IsLocked;
        
        NewTabCommand = new DelegateCommand(_ => newTabEvent?.Invoke(this));
        DuplicateTabCommand = new DelegateCommand(_ => duplicateTabEvent?.Invoke(this));
        ToggleLockCommand = new DelegateCommand(_ => IsLocked = !IsLocked);
        
        IpListViewModel.DataGridViewModel.Items = data.CreateIpListDataTable();

        _tabParameterSet = data.CreateTabParameterSet();
        ScriptPageViewModel.Initialize(IpListViewModel,_tabParameterSet);
        ScriptPageViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ScriptPageViewModel.CurrentPage))
            {
                Header = ScriptPageViewModel.CurrentPage == ScriptPageViewModel.Page.SelectScript
                    ? "Select Script"
                    : ScriptPageViewModel.CurrentScriptRunViewModel.ScriptName;
            }
        };
    }

    public MainTabViewModel() : this(new MainTabData())
    {
    }
    
    public MainTabData CreateMainTabData()
    {
        return new MainTabData(IpListViewModel.DataGridViewModel.Items, _tabParameterSet)
        {
            TabHeader = Header
        };
    }
}