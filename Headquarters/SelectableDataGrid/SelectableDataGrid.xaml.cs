using System.Windows;
using System.Windows.Controls;

namespace Headquarters;

public partial class SelectableDataGrid
{
    public SelectableDataGrid()
    {
        DataContext = new SelectableDataGridViewModel();
        InitializeComponent();
    }

    private void TargetDataGrid_OnAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyName != SelectableDataGridViewModel.SelectedPropertyName) return;

        var c = new DataGridTemplateColumn()
        {
            Header = e.Column.Header,
            HeaderTemplate = TargetDataGrid.Resources["IsSelectedHeader"] as DataTemplate,
            HeaderStringFormat = e.Column.HeaderStringFormat,
            CellTemplate = TargetDataGrid.Resources["IsSelected"] as DataTemplate,
            CanUserSort = false,
            CellStyle = TargetDataGrid.Resources["IsSelectedColumnCellStyle"] as Style
        };
        e.Column = c;
    }
}