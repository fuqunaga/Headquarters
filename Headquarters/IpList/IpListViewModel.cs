using System.Windows.Input;

namespace Headquarters;

public class IpListViewModel : ViewModelBase
{
    public IpListDataGridViewModel DataGridViewModel { get; } = new();
    
    public ICommand ExportCommand { get; protected set; }
    public ICommand ImportCommand { get; protected set; }
    
    public IpListViewModel()
    {
        // TODO: Implement
        ExportCommand = new DelegateCommand(Export);
        ImportCommand = new DelegateCommand(Import);
    }

    private void Import(object? obj)
    {
        throw new System.NotImplementedException();
    }

    private void Export(object? obj)
    {
        throw new System.NotImplementedException();
    }
}