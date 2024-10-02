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
            CanUserSort = false
        };
        e.Column = c;
    }
}