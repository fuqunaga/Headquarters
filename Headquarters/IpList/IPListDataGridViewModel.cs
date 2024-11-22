using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Headquarters;

public class IpListDataGridViewModel : SelectableDataGridViewModel
{
    private bool _isLocked;
    
    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }
    
    public ICommand AddColumnCommand { get; }
    public ICommand RenameColumnCommand { get; }
    public ICommand DeleteColumnCommand { get; }


    public IpListDataGridViewModel()
    {
        // CS4014: Because this call is not awaited, execution of the current method continues before the call is completed.
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs4014?redirectedfrom=MSDN
        AddColumnCommand = new DelegateCommand(_ => {var suppressWarning = AddColumn();}, _ => !IsLocked);
        RenameColumnCommand = new DelegateCommand(o => {var suppressWarning = RenameColumn(o); }, (obj) => !IsLocked && IsColumnNameEditable(obj));
        DeleteColumnCommand = new DelegateCommand(o => {var suppressWarning = DeleteColumn(o); }, (obj) => !IsLocked && IsColumnNameEditable(obj));
    }

    private bool IsColumnNameEditable(object? obj)
    {
        var columnName = GetColumnNameFromMenuItem(obj);
        return (columnName != SelectedPropertyName) && (columnName != IpParameterSet.IpPropertyName);
    }

    private string GetColumnNameFromMenuItem(object? obj)
    {
        if ( obj is not DataGridColumnHeader header )
        {
            return string.Empty;
        }
        
        return (string)header.Content;
    }


    private async Task AddColumn()
    {
        var (success, name) = await ShowColumnNameDialog("Add Column", "Add");
        if (!success) return;
        if (Items.Columns.Contains(name)) return;
            
        Items.Columns.Add(name);
        RefreshDataGrid();
    }

    private async Task RenameColumn(object? obj)
    {
        var name = GetColumnNameFromMenuItem(obj);
        
        var (success, newName) = await ShowColumnNameDialog("Rename Column", "Rename", name);
        if (!success) return;
        if (Items.Columns.Contains(newName)) return;

        var column = Items.Columns[name];
        if (column != null)
        {
            column.ColumnName = newName;
            RefreshDataGrid();
        }
    }

    private async Task DeleteColumn(object? obj)
    {
        var name = GetColumnNameFromMenuItem(obj);

        var (success, _) = await ShowColumnNameDialog("Delete Column", "Delete",  name, false);
        if (!success) return;
        
        if (Items.Columns.Contains(name))
        {
            Items.Columns.Remove(name);
            RefreshDataGrid();
        }
    }

    private async Task<(bool success, string)> ShowColumnNameDialog(string? title, string okButtonContent, string? name = null, bool isEnabled = true)
    {
        var viewModel = new NameDialogViewModel()
        {
            Title = title,
            OkButtonContent = okButtonContent,
            Name = name,
            IsEnabled = isEnabled
        };
        
        var validationRule = new NotContainDataColumnCollectionValidationRule(Items.Columns, "Column already exists.");
        
        return await NameDialogService.ShowDialog(viewModel, validationRule);
    }
 


    public IEnumerable<IpParameterSet> IpParams => Items.Rows.OfType<DataRow>().Select(d => new IpParameterSet(d));
    public IEnumerable<IpParameterSet> SelectedParams => IpParams.Where(p => p.IsSelected);
    public bool Contains(string name) => Items.Columns.Contains(name);
}