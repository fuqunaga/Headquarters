using System.Windows;

namespace Headquarters;

public partial class IpListBar
{
    private IpListBarViewModel ViewModel => DataContext as IpListBarViewModel;
    
    public IpListBar() => InitializeComponent();

    private void SaveAs(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveAs();
    }
}