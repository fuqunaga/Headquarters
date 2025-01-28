using System;
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

    public Func<IEnumerable<string>>? getScriptParameterNamesFunc;
  
    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }
    
    public ICommand AddColumnCommand { get; }
    public ICommand RenameColumnCommand { get; }
    public ICommand DeleteColumnCommand { get; }

    public IEnumerable<IpParameterSet> IpParams => Items.Rows.OfType<DataRow>().Select(d => new IpParameterSet(d));
    public IEnumerable<IpParameterSet> SelectedParams => IpParams.Where(p => p.IsSelected);
    public bool Contains(string name) => Items.Columns.Contains(name);
    

    public IpListDataGridViewModel()
    {
        // CS4014: Because this call is not awaited, execution of the current method continues before the call is completed.
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs4014?redirectedfrom=MSDN
        AddColumnCommand = new DelegateCommand(_ => {var suppressWarning = AddColumn();}, _ => !IsLocked);
        RenameColumnCommand = new DelegateCommand(o => {var suppressWarning = RenameColumn(o); }, (obj) => !IsLocked && IsColumnNameEditable(obj));
        DeleteColumnCommand = new DelegateCommand(o => {var suppressWarning = DeleteColumn(o); }, (obj) => !IsLocked && IsColumnNameEditable(obj));
    }

    private static bool IsColumnNameEditable(object? obj)
    {
        var columnName = GetColumnNameFromMenuItem(obj);
        return (columnName != SelectedPropertyName) && (columnName != IpParameterSet.IpPropertyName);
    }

    private static string GetColumnNameFromMenuItem(object? obj)
    {
        if ( obj is not DataGridColumnHeader header )
        {
            return string.Empty;
        }
        
        return (string)header.Content;
    }

    private IEnumerable<string> GetScriptParameterNamesWithoutColumnNames()
    {
        return getScriptParameterNamesFunc?.Invoke().Except(Items.Columns.OfType<DataColumn>().Select(c => c.ColumnName))
            ?? Array.Empty<string>();
    }

    private async Task AddColumn()
    {
        var viewModel = new NameDialogViewModel()
        {
            Title = "Add Column",
            OkButtonContent = "Add",
            Suggestions = GetScriptParameterNamesWithoutColumnNames(),
        };
        var (success, name) = await ShowColumnNameDialog(viewModel);
        if (!success) return;
        if (Items.Columns.Contains(name)) return;
            
        Items.Columns.Add(name);
        RefreshDataGrid();
    }

    private async Task RenameColumn(object? obj)
    {
        var name = GetColumnNameFromMenuItem(obj);
        
        var viewModel = new NameDialogViewModel()
        {
            Title = "Rename Column",
            OkButtonContent = "Rename",
            Name = name,
            Suggestions = GetScriptParameterNamesWithoutColumnNames(),
        };
        var (success, newName) = await ShowColumnNameDialog(viewModel);
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

        var viewModel = new NameDialogViewModel()
        {
            Title = "Delete Column",
            OkButtonContent = "Delete",
            Name = name,
            IsEnabled = false
        };
        var (success, _) = await ShowColumnNameDialog(viewModel);
        if (!success) return;
        
        if (Items.Columns.Contains(name))
        {
            Items.Columns.Remove(name);
            RefreshDataGrid();
        }
    }

    private async Task<(bool success, string)> ShowColumnNameDialog(NameDialogViewModel viewModel)
    {
        var validationRule = new NotContainDataColumnCollectionValidationRule(Items.Columns, "Column already exists.");
        return await NameDialogService.ShowDialog(viewModel, validationRule);
    }
}