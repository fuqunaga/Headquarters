namespace Headquarters;

public class MainTabViewModel : ViewModelBase
{
    public IpListViewModel IpListViewModel { get; } = new();
    public ScriptPageViewModel ScriptPageViewModel { get; } = new();
    
    public string Header { get; }
    

    public MainTabViewModel(MainTabData data)
    {
        Header = data.TabHeader;
        IpListViewModel.DataGridViewModel.Items = data.CreateIpListDataTable();
        
        ScriptPageViewModel.SetIpListViewModel(IpListViewModel);
    }

    private MainTabViewModel() : this(new MainTabData())
    {
    }
    
    public MainTabData CreateMainTabData()
    {
        return new MainTabData(IpListViewModel.DataGridViewModel.Items)
        {
            TabHeader = Header
        };
    }
}