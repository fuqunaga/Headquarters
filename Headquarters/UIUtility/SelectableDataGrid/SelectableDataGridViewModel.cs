﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Input;

namespace Headquarters;

public class SelectableDataGridViewModel : ViewModelBase
{
    private static DataTable? _tempDataTableForRefresh;
        
    public const string SelectedPropertyName = "IsSelected";
        
    private DataTable _items = new();

    public ICommand AddRowCommand { get; }
    
    public DataTable Items
    {
        get => _items;
        set
        {
            if (EqualityComparer<DataTable>.Default.Equals(_items, value))
            {
                return;
            }

            _items = value;
            VerifyAndSettingSelectedColumnIfNeed();
            AddItemsCallback();
            
            OnPropertyChanged();
        }
    }
    
    protected virtual bool IsAddRowCommandEnabled => true;

    public SelectableDataGridViewModel()
    {
        AddRowCommand = new DelegateCommand(_ =>
        {
            var newRow = Items.NewRow();
            Items.Rows.Add(newRow);
        },
        _ => IsAddRowCommandEnabled);
    }

    private void VerifyAndSettingSelectedColumnIfNeed()
    {
        var selectedColumn = Items.Columns[SelectedPropertyName];
        if (selectedColumn == null)
        {
            selectedColumn = Items.Columns.Add(SelectedPropertyName, typeof(bool));
            selectedColumn.SetOrdinal(0);
        }

        foreach (DataRow row in Items.Rows)
        {
            if (row[SelectedPropertyName] is not bool)
            {
                row[SelectedPropertyName] = false;
            }
        }
                
        selectedColumn.DefaultValue = false;
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
        
        Items.RowDeleted += (_, _) =>
        {
            OnPropertyChanged(nameof(IsAllItemSelected));
        };
        
        Items.RowChanged += (_, e) =>
        {
            if (e.Action is DataRowAction.Add)
            {
                OnPropertyChanged(nameof(IsAllItemSelected));
            }
        };
    }

    public bool? IsAllItemSelected
    {
        get
        {
            var list = Items.Rows.Cast<DataRow>().Select(row => row[SelectedPropertyName]).Cast<bool>();

            var uniqList = list.Distinct().ToList();
            return uniqList.Count switch
            {
                0 => false,
                1 => uniqList.Single(),
                _ => null
            };
        }
        set
        {
            if (!value.HasValue) return;
                
            foreach (var row in Items.Rows.Cast<DataRow>())
            {
                row[SelectedPropertyName] = value;
            }
            OnPropertyChanged();
        }
    }
        
    // https://stackoverflow.com/questions/36215919/datatable-is-not-updating-datagrid-after-clearing-and-refilling-data-mvvm
    protected void RefreshDataGrid()
    {
        _tempDataTableForRefresh ??= new DataTable();
            
        var temp = Items;
        Items = _tempDataTableForRefresh;
        Items = temp;
    }
}