using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Headquarters
{
    class IPListViewModel : DataGridWithSelectAll
    {
        #region Singleton

        public static IPListViewModel Instance { get; } = new IPListViewModel();

        private IPListViewModel() : base()
        {
            IPParams.isSelectedPropertyName = selectedPropertyName;
        }

        #endregion


        protected bool isColumnEditable_;
        public bool IsColumnEditable
        {
            get => isColumnEditable_;
            protected set
            {
                if (isColumnEditable_ != value)
                {
                    isColumnEditable_ = value;
                    OnPropertyChanged(nameof(IsColumnEditable));
                }
            }
        }

        protected string filepath;

        public void Load(string filepath)
        {
            this.filepath = filepath;

            Items = new DataTable();
            Items.Columns.Add(selectedPropertyName, typeof(bool));

            var lines = File.Exists(filepath) ? File.ReadAllLines(filepath) : new string[0];

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
                MessageBox.Show($"{filepath}が不正です。", "ipList error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


            AddItemsCallback();
        }

        public IEnumerable<IPParams> ipParams => Items.Rows.OfType<DataRow>().Select(d => new IPParams(d));
        public IEnumerable<IPParams> selectedParams => ipParams.Where(p => p.isSelected);

        public bool Contains(string name) => Items.Columns.Contains(name);

        internal void Save()
        {
            var csv = "";

            var names = Items.Columns.OfType<DataColumn>()
                .Select(c => c.ColumnName)
                .Where(name => name != selectedPropertyName)
                .ToList();

            csv = string.Join(",", names.ToArray()) + Environment.NewLine;

            csv += string.Join(Environment.NewLine,
                Items.Rows.OfType<DataRow>().Select(row =>
                {
                    var hoge = names.Select(name => row[name].ToString()).ToArray();
                    return string.Join(",", hoge);
                }).ToArray());


            File.WriteAllText(filepath, csv);
        }





        internal void OnHeaderContextMenuOpen(object sender)
        {
            var header = (DataGridColumnHeader)sender;
            var name = (string)header.Content;

            IsColumnEditable = !((name == selectedPropertyName) || (name == IPParams.ipPropertyName));
        }

        public async void AddColumn(object o)
        {
            var vm = new NameDialogViewModel()
            {
                Title = "Add Column:"
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
                    Items.Columns.Add(vm.Name, typeof(string));
                    RefreshDataGrid();
                }
            }
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
            dataGridBinding.DataContext = null;
            dataGridBinding.DataContext = this;

            OnPropertyChanged(nameof(Items));
        }
    }
}
