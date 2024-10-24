using System;

namespace Headquarters;

public class MainTabViewModel : ViewModelBase
{
    public static Func<MainTabViewModel> Factory => () => new MainTabViewModel();

    private readonly TabParameterSet _tabParameterSet;
    private string _header;
    
    public IpListViewModel IpListViewModel { get; } = new();
    public ScriptPageViewModel ScriptPageViewModel { get; } = new();

    public string Header
    {
        get => _header;
        set => SetProperty(ref _header, value);
    }

    public MainTabViewModel(MainTabData data)
    {
        _header = data.TabHeader;
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