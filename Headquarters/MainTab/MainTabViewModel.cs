using System;
using System.Windows.Input;
using Dragablz;

namespace Headquarters;

public class MainTabViewModel : ViewModelBase
{
    public static Func<MainTabViewModel> Factory => () => new MainTabViewModel();

    private readonly TabParameterSet _tabParameterSet;
    private string _header;
    private bool _isLocked;

    public ICommand NewTabCommand { get; }
    public ICommand DuplicateTabCommand { get; }
    public ICommand ToggleLockCommand { get; private set; }
    public ICommand CloseTabCommand { get; private set; }

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
        
        NewTabCommand = new DelegateCommand(_ => NewTab(this));
        DuplicateTabCommand = new DelegateCommand(_ => DuplicateTab(this));
        ToggleLockCommand = new DelegateCommand(_ => IsLocked = !IsLocked);
        CloseTabCommand = new DelegateCommand(_ => TabablzControl.CloseItem(this), _ => !IsLocked);
        
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
    
        
    private static void NewTab(MainTabViewModel sender)
    {
        var newItem = new MainTabViewModel();
        TabablzControl.AddItem(newItem, sender, AddLocationHint.After);
        TabablzControl.SelectItem(newItem);
    }

    private static void DuplicateTab(MainTabViewModel sender)
    {
        var data = sender.CreateMainTabData();
        var newItem = new MainTabViewModel(data);
        TabablzControl.AddItem(newItem, sender, AddLocationHint.After);
        TabablzControl.SelectItem(newItem);
    }
}