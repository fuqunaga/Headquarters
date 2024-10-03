using System.Collections.ObjectModel;

namespace Headquarters;

public class MainWindowViewModel
{
    public ObservableCollection<MainTabViewModel> TabItems { get; set; } = new();
}