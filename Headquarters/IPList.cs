using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Headquarters
{
    class IPList : DataGridWithSelectAll
    {
        public IPList(string filepath)
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
        }
    }
}
