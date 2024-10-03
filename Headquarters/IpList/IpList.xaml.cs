using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Headquarters;

public partial class IpList
{
    private const string IpListFolder = @".\IPList";
    
    public IpList()
    {
        InitializeComponent();
        DataContext = new IpListViewModel();
    }

    public void SaveAs(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            Title = "Save IP List file",
            InitialDirectory = Path.GetFullPath(IpListFolder)
        };

        dialog.ShowDialog();
    }

    private void OnClickAddColumn(object sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void OnClickRenameColumn(object sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void OnClickDeleteColumn(object sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void OnHeaderContextMenuOpen(object sender, ContextMenuEventArgs e)
    {
        throw new System.NotImplementedException();
    }
}