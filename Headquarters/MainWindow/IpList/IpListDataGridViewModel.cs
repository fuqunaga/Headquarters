using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Headquarters;

public class IpListDataGridViewModel : SelectableDataGridViewModel
{
    private bool _isEnabled = true;
    private bool _isLocked;

    public Func<IEnumerable<string>>? getScriptParameterNamesFunc;
  
    // スクリプト連続実行中はチェックも編集もロックする
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
    
    // タブロック中は編集のみロックする。チェックは変更可
    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }

    protected override bool IsAddRowCommandEnabled => !IsLocked;

    public ICommand AddColumnCommand { get; }
    public ICommand RenameColumnCommand { get; }
    public ICommand DeleteColumnCommand { get; }

    public IEnumerable<IpParameterSet> IpParams => Items.Rows.OfType<DataRow>().Select(d => new IpParameterSet(d));
    public IEnumerable<IpParameterSet> SelectedParams => IpParams.Where(p => p.IsSelected);
    public bool Contains(string name) => Items.Columns.Contains(name);
    

    public IpListDataGridViewModel()
    {
        AddColumnCommand = new DelegateCommand(_ =>  AddColumn(), _ => !IsLocked);
        RenameColumnCommand = new DelegateCommand(RenameColumn, (obj) => !IsLocked && IsColumnNameEditable(obj));
        DeleteColumnCommand = new DelegateCommand(DeleteColumn, (obj) => !IsLocked && IsColumnNameEditable(obj));
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

    private async void AddColumn()
    {
        var viewModel = new ComboBoxDialogViewModel()
        {
            Title = "Add Column",
            OkButtonContent = "Add",
            Suggestions = GetScriptParameterNamesWithoutColumnNames(),
        };
        viewModel.AddValidator(
            new NotContainDataColumnCollectionValidator(Items.Columns, "Column already exists.")
        );
        
        var success = await DialogService.ShowDialog(viewModel);
        if (!success) return;
        
        var name = viewModel.Text;
        if (Items.Columns.Contains(name)) return;
            
        Items.Columns.Add(name);
        RefreshDataGrid();
    }

    private async void RenameColumn(object? obj)
    {
        var name = GetColumnNameFromMenuItem(obj);
        
        var viewModel = new ComboBoxDialogViewModel()
        {
            Title = "Rename Column",
            OkButtonContent = "Rename",
            Text = name,
            Suggestions = GetScriptParameterNamesWithoutColumnNames(),
        };
        viewModel.AddValidator(
            new NotContainDataColumnCollectionValidator(Items.Columns, "Column already exists.")
        );
        
        var success = await DialogService.ShowDialog(viewModel);
        if (!success) return;
        
        var newName = viewModel.Text;
        if (Items.Columns.Contains(newName)) return;

        var column = Items.Columns[name];
        if (column != null)
        {
            column.ColumnName = newName;
            RefreshDataGrid();
        }
    }

    private async void DeleteColumn(object? obj)
    {
        var name = GetColumnNameFromMenuItem(obj);

        var viewModel = new LabelDialogViewModel()
        {
            Title = "Delete Column",
            OkButtonContent = "Delete",
            Text = $"{name} 列を削除しますか？",
        };
        var success = await DialogService.ShowDialog(viewModel);
        if (!success) return;
        
        if (Items.Columns.Contains(name))
        {
            Items.Columns.Remove(name);
            RefreshDataGrid();
        }
    }
    
    public void AddRowIfNoItems()
    {
        if (Items.Rows.Count == 0)
        {
            AddRowCommand.Execute(null);
        }
    }
}