using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace Headquarters
{
    public class SelectableDataGridViewModel : ViewModelBase
    {
        public DataTable Items { get; set; }


        public const string SelectedPropertyName = "IsSelected";


        public SelectableDataGridViewModel()
        {
            Items = new DataTable();
            Items.Columns.Add(SelectedPropertyName, typeof(bool));
            

            // if (Items.Columns[IPParams.ipPropertyName] == null)
            // {
            //     Items.Columns.Add(IPParams.ipPropertyName, typeof(string));
            // }
            
            AddItemsCallback();
        }
        
        protected void AddItemsCallback()
        {
            Items.ColumnChanged += (s, e) =>
            {
                if (e.Column?.ColumnName == SelectedPropertyName)
                {
                    OnPropertyChanged(nameof(IsAllItemSelected));
                }
            };

            Items.RowChanged += (s, e) =>
            {
                if (e.Action == DataRowAction.Add)
                {
                    e.Row[SelectedPropertyName] = true;
                    Debug.WriteLine(e);
                }
            };
        }

        public bool? IsAllItemSelected
        {
            get
            {
                var list = Items.AsEnumerable().Select(row => row[SelectedPropertyName]).Cast<bool>();

                var uniqList = list.Distinct().ToList();
                return (uniqList.Count == 1) ? (bool?)uniqList.Single() : null;
            }
            set
            {
                if (!value.HasValue) return;
                
                foreach (var row in Items.AsEnumerable())
                {
                    row[SelectedPropertyName] = value;
                }
                OnPropertyChanged();
            }
        }
        
    }
}
