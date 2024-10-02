using System.Data;
using System.Diagnostics;
using System.Linq;

namespace Headquarters;

public class SelectableDataGridViewModel : ViewModelBase
{
    private static DataTable? _tempDataTableForRefresh;
        
    public const string SelectedPropertyName = "IsSelected";
        
    private DataTable _items;

    public DataTable Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }


    public SelectableDataGridViewModel()
    {
        _items = new DataTable();
        _items.Columns.Add(SelectedPropertyName, typeof(bool));
            

        // if (Items.Columns[IPParams.ipPropertyName] == null)
        // {
        //     Items.Columns.Add(IPParams.ipPropertyName, typeof(string));
        // }
            
        AddItemsCallback();
    }
        
    protected void AddItemsCallback()
    {
        Items.ColumnChanged += (_, e) =>
        {
            if (e.Column?.ColumnName == SelectedPropertyName)
            {
                OnPropertyChanged(nameof(IsAllItemSelected));
            }
        };

        Items.RowChanged += (_, e) =>
        {
            if (e.Action == DataRowAction.Add)
            {
                e.Row[SelectedPropertyName] = true;
                Debug.WriteLine(e);
            }
        };
    }

    public bool? IsAllItemSelected
    {
        get
        {
            var list = Items.AsEnumerable().Select(row => row[SelectedPropertyName]).Cast<bool>();

            var uniqList = list.Distinct().ToList();
            return (uniqList.Count == 1) ? uniqList.Single() : null;
        }
        set
        {
            if (!value.HasValue) return;
                
            foreach (var row in Items.AsEnumerable())
            {
                row[SelectedPropertyName] = value;
            }
            OnPropertyChanged();
        }
    }
        
    // https://stackoverflow.com/questions/36215919/datatable-is-not-updating-datagrid-after-clearing-and-refilling-data-mvvm
    public void RefreshDataGrid()
    {
        _tempDataTableForRefresh ??= new DataTable();
            
        var temp = Items;
        Items = _tempDataTableForRefresh;
        Items = temp;
    }
}