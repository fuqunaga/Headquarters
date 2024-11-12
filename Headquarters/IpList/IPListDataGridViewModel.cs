﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Headquarters;

public class IpListDataGridViewModel : SelectableDataGridViewModel
{
    public ICommand AddColumnCommand { get; }
    public ICommand RenameColumnCommand { get; }
    public ICommand DeleteColumnCommand { get; }


    public IpListDataGridViewModel()
    {
        AddColumnCommand = new DelegateCommand(_ => AddColumn());
        RenameColumnCommand = new DelegateCommand(RenameColumn, IsColumnNameEditable);
        DeleteColumnCommand = new DelegateCommand(DeleteColumn, IsColumnNameEditable);
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
    
    
    public async void AddColumn()
    {
        var (success, name) = await ShowColumnNameDialog("Add Column");
        if (!success) return;
        if (Items.Columns.Contains(name)) return;
            
        Items.Columns.Add(name);
        RefreshDataGrid();
    }

    public async void RenameColumn(object? obj)
    {
        var name = GetColumnNameFromMenuItem(obj);
        
        var (success, newName) = await ShowColumnNameDialog("Rename Column", name);
        if (!success) return;
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

        var (success, _) = await ShowColumnNameDialog("Delete Column", name, false);
        if (!success) return;
        
        if (Items.Columns.Contains(name))
        {
            Items.Columns.Remove(name);
            RefreshDataGrid();
        }
    }

    private async Task<(bool success, string)> ShowColumnNameDialog(string? title, string? name = null, bool isEnabled = true)
    {
        var viewModel = new NameDialogViewModel()
        {
            Title = title,
            Name = name,
            IsEnabled = isEnabled
        };
        
        var validationRule = new NotContainDataColumnCollectionValidationRule(Items.Columns, "Column already exists.");
        
        return await NameDialogService.ShowDialog(viewModel, validationRule);
    }
 


    public IEnumerable<IpParameterSet> IpParams => Items.Rows.OfType<DataRow>().Select(d => new IpParameterSet(d));
    public IEnumerable<IpParameterSet> SelectedParams => IpParams.Where(p => p.IsSelected);

    public bool Contains(string name) => Items.Columns.Contains(name);



    public void Load(string filePath)
    {
        Items = new DataTable();
        Items.Columns.Add(SelectedPropertyName, typeof(bool));

        var lines = File.Exists(filePath) ? File.ReadAllLines(filePath) : new string[0];

        lines.FirstOrDefault()?.Split(',').ToList().ForEach(header => Items.Columns.Add(header, typeof(string)));

        if (Items.Columns[IpParameterSet.IpPropertyName] == null)
        {
            Items.Columns.Add(IpParameterSet.IpPropertyName, typeof(string));
        }

        try
        {
            for (var i = 1; i < lines.Length; ++i)
            {
                var rows = new[] { (object)true }.Concat(lines[i].Split(',')).ToArray();
                Items.Rows.Add(rows);
            }
        }
        catch
        {
            MessageBox.Show($"{filePath}が不正です。", "ipList error", MessageBoxButton.OK, MessageBoxImage.Error);
        }


        AddItemsCallback();
    }

    public void Save(string filePath)
    {
        var names = Items.Columns.OfType<DataColumn>()
            .Select(c => c.ColumnName)
            .Where(name => name != SelectedPropertyName)
            .ToList();

        var csv = string.Join(",", names) + Environment.NewLine;

        csv += string.Join(Environment.NewLine, Items.Rows.OfType<DataRow>().Select(row =>
        {
            var element = names.Select(name => row[name].ToString());
            return string.Join(",", element);
        }));


        var fileInfo = new FileInfo(filePath);
        fileInfo.Directory?.Create();
        File.WriteAllText(filePath, csv);
    }

    
}