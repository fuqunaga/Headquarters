namespace Headquarters;

public class MainTabViewModel : ViewModelBase
{
    private readonly TabParameterSet _tabParameterSet;
    
    public IpListViewModel IpListViewModel { get; } = new();
    public ScriptPageViewModel ScriptPageViewModel { get; } = new();
    
    public string Header { get; }
    

    public MainTabViewModel(MainTabData data)
    {
        Header = data.TabHeader;
        IpListViewModel.DataGridViewModel.Items = data.CreateIpListDataTable();

        _tabParameterSet = data.CreateTabParameterSet();
        ScriptPageViewModel.Initialize(IpListViewModel,_tabParameterSet);
    }

    private MainTabViewModel() : this(new MainTabData())
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