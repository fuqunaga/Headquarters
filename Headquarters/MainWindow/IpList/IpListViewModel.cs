using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace Headquarters;

public class IpListViewModel : ViewModelBase
{
    private string _lastImportedFilePath = string.Empty;
    private bool _isLocked;

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if ( SetProperty(ref _isLocked, value) )
            {
                DataGridViewModel.IsLocked = value;
            }
        }
    }

    public IpListDataGridViewModel DataGridViewModel { get; } = new();
    
    public ICommand ExportCommand { get; protected set; }
    public ICommand ImportCommand { get; protected set; }
    
    public IpListViewModel()
    {
        ExportCommand = new DelegateCommand(Export);
        ImportCommand = new DelegateCommand(Import);
    }

    private void Import(object? _)
    {
        var dialog = new OpenFileDialog()
        {
            Filter = "CSVファイル(*.csv)|*.csv",
            Title = "CSVファイルを選択してください",
            CheckFileExists = true,
            Multiselect = false
        };

        var success = dialog.ShowDialog();
        if (success != true) return;
        
        var filePath = dialog.FileName;

        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0)
        {
            MessageBox.Show($"{filePath}が空です。", "ipList error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        
        var dataTable = new DataTable();

        foreach (var header in lines.First().Split(','))
        {
            dataTable.Columns.Add(header, typeof(string));
        }

        if (dataTable.Columns[IpParameterSet.IpPropertyName] == null)
        {
            dataTable.Columns.Add(IpParameterSet.IpPropertyName, typeof(string));
        }

        try
        {
            for (var i = 1; i < lines.Length; ++i)
            {
                var rows = lines[i].Split(',').ToArray<object?>();
                dataTable.Rows.Add(rows);
            }
        }
        catch
        {
            MessageBox.Show($"{filePath}が不正です。", "ipList error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        SetItems(dataTable);
        _lastImportedFilePath = filePath;
    }

    private void Export(object? _)
    {
        var dialog = new SaveFileDialog()
        {
            Filter = "CSVファイル(*.csv)|*.csv",
            Title = "CSVファイルを保存する場所を選択してください",
            FileName = _lastImportedFilePath
        };

        var success = dialog.ShowDialog();
        if (success != true) return;

        var filePath = dialog.FileName;
        
        var dataTable = DataGridViewModel.Items;
        var names = dataTable.Columns.OfType<DataColumn>()
            .Select(c => c.ColumnName)
            .Where(name => name != SelectableDataGridViewModel.SelectedPropertyName)
            .ToList();

        var header = string.Join(",", names);
        var dataLines = dataTable.Rows.OfType<DataRow>().Select(row =>
        {
            var element = names.Select(name => row[name].ToString());
            return string.Join(",", element);
        });

        var csv = string.Join(Environment.NewLine, dataLines.Prepend(header));


        var fileInfo = new FileInfo(filePath);
        fileInfo.Directory?.Create();
        File.WriteAllText(filePath, csv);
    }

    public void SetItems(DataTable dataTable)
    {
        DataGridViewModel.Items = dataTable;
        DataGridViewModel.AddRowIfNoItems();
    }
}