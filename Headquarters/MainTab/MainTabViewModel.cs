namespace Headquarters;

public class MainTabViewModel : ViewModelBase
{
    private readonly MainTabModel _model;
    
    public IpListViewModel IpListViewModel { get; } = new();
    
    public string Header => _model.Header;
    

    public MainTabViewModel(MainTabModel model)
    {
        _model = model;
        IpListViewModel.DataGridViewModel.Items = _model.IpListDataTable;
    }

    private MainTabViewModel() : this(new MainTabModel())
    {
    }
}