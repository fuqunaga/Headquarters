using System;
using System.Windows;
using System.Windows.Controls;

namespace Headquarters;

public partial class IpListDataGrid : UserControl
{
    public IpListDataGrid()
    {
        InitializeComponent();
        DataContext = IpListDataGridViewModel.Instance;
    }

    private void TargetDataGrid_OnAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyName != SelectableDataGridViewModel.SelectedPropertyName) return;

        var c = new DataGridTemplateColumn()
        {
            CellTemplate = TargetDataGrid.Resources["IsSelected"] as DataTemplate,
            Header = e.Column.Header,
            HeaderTemplate = TargetDataGrid.Resources["IsSelectedHeader"] as DataTemplate,
            HeaderStringFormat = e.Column.HeaderStringFormat,
            CanUserSort = false
        };
        e.Column = c;
    }

    private void AddColumn(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("AddColumn");
        throw new System.NotImplementedException();
    }
}