using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
    }
}
