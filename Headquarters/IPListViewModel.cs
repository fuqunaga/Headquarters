using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Headquarters
{
    class IPListViewModel : DataGridWithSelectAll
    {
        #region Singleton

        public static IPListViewModel Instance { get; } = new IPListViewModel();

        private IPListViewModel() : base()
        {
        }

        #endregion

        public void Load(string filepath)
        {
            var lines = File.ReadAllLines(filepath);

            Items = new DataTable();
            Items.Columns.Add(selectedPropertyName, typeof(bool));
            lines.First().Split(',').ToList().ForEach(header => Items.Columns.Add(header, typeof(string)));

            for (var i = 1; i < lines.Length; ++i)
            {
                var rows = new[] { (object)false }.Concat(lines[i].Split(',')).ToArray();
                Items.Rows.Add(rows);
            }

            AddItemsCallback();

            IPParams.isSelectedPropetyName = selectedPropertyName;
        }

        public IEnumerable<IPParams> ipParams => Items.Rows.OfType<DataRow>().Select(d => new IPParams(d));
        public IEnumerable<IPParams> selectedParams => ipParams.Where(p => p.isSelected);

        public bool Contains(string name) => Items.Columns.Contains(name);
    }
}
