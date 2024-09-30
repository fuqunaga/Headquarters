using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Headquarters;

public class IPListViewModel : SelectableDataGridViewModel
{
    #region Singleton

    public static IPListViewModel Instance { get; } = new IPListViewModel();

    private IPListViewModel() : base()
    {
        IPParams.isSelectedPropertyName = SelectedPropertyName;
    }

    #endregion


    protected bool isColumnEditable;

    public bool IsColumnEditable
    {
        get => isColumnEditable;
        protected set
        {
            if (isColumnEditable != value)
            {
                isColumnEditable = value;
                OnPropertyChanged(nameof(IsColumnEditable));
            }
        }
    }


    public IEnumerable<IPParams> IpParams => Items.Rows.OfType<DataRow>().Select(d => new IPParams(d));
    public IEnumerable<IPParams> SelectedParams => IpParams.Where(p => p.isSelected);

    public bool Contains(string name) => Items.Columns.Contains(name);



    public void Load(string filePath)
    {
        Items = new DataTable();
        Items.Columns.Add(SelectedPropertyName, typeof(bool));

        var lines = File.Exists(filePath) ? File.ReadAllLines(filePath) : new string[0];

        lines.FirstOrDefault()?.Split(',').ToList().ForEach(header => Items.Columns.Add(header, typeof(string)));

        if (Items.Columns[IPParams.ipPropertyName] == null)
        {
            Items.Columns.Add(IPParams.ipPropertyName, typeof(string));
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


    internal void OnHeaderContextMenuOpen(object sender)
    {
        var header = (DataGridColumnHeader)sender;
        var name = (string)header.Content;

        IsColumnEditable = !((name == SelectedPropertyName) || (name == IPParams.ipPropertyName));
    }

    [SupportedOSPlatform("windows")]
    public async void AddColumn(object o)
    {
        var vm = new NameDialogViewModel()
        {
            Title = "Add Column"
        };

        var view = new NameDialog()
        {
            DataContext = vm
        };
            
        var binding = BindingOperations.GetBinding(view.NameTextBox, TextBox.TextProperty);
        if (binding != null)
        {
            var validationRule = new NotContainDataColumnCollectionValidationRule(Items.Columns, "Column already exists.");
            binding.ValidationRules.Add(validationRule);
        }

        var result = await DialogHost.Show(view, "RootDialog");

        if (result == null || !(bool)result) return;
        if (Items.Columns.Contains(vm.Name)) return;
            
        Items.Columns.Add(vm.Name, typeof(string));
        RefreshDataGrid();
    }

    public async void RenameColumn(object sender)
    {
        var item = (MenuItem)sender;
        var contextMenu = (ContextMenu)item.Parent;
        var header = (DataGridColumnHeader)contextMenu.PlacementTarget;
        var name = (string)header.Content;


        var vm = new NameDialogViewModel()
        {
            Title = "Rename Column:",
            Name = name
        };

        var view = new NameDialog()
        {
            DataContext = vm
        };

        var result = await DialogHost.Show(view, "RootDialog");

        if ((bool)result)
        {
            if (!Items.Columns.Contains(vm.Name))
            {
                var column = Items.Columns[name];
                column.ColumnName = vm.Name;

                RefreshDataGrid();
            }
        }
    }

    internal void DeleteColumn(object sender)
    {
        var item = (MenuItem)sender;
        var contextMenu = (ContextMenu)item.Parent;
        var header = (DataGridColumnHeader)contextMenu.PlacementTarget;

        Items.Columns.Remove((string)header.Content);
        RefreshDataGrid();
    }

    protected void RefreshDataGrid()
    {
        // refresh datagrid
        // https://code.msdn.microsoft.com/windowsdesktop/How-to-add-the-Column-into-2ad31c47
        // dataGridBinding.DataContext = null;
        // dataGridBinding.DataContext = this;

        OnPropertyChanged(nameof(Items));
    }
}