using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Headquarters
{
    class DataGridWithSelectAll : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DataTable Items { get; set; }


        public virtual string selectedPropertyName => "IsSelected";
        public virtual string cellTemplateKey => "IsSelected";
        public virtual string headerTemplateKey => "SelectAll";


        protected DataGrid dataGridBinding;

        protected void AddItemsCallback()
        {
            Items.ColumnChanged += (s, e) =>
            {
                if (e.Column.ColumnName == selectedPropertyName)
                {
                    OnPropertyChanged(selectedPropertyName);
                }
            };

            Items.RowChanged += (s, e) =>
            {
                if (e.Action == DataRowAction.Add)
                {
                    e.Row[selectedPropertyName] = true;
                    Debug.WriteLine(e);
                }
            };
        }

        public virtual void Bind(DataGrid dataGrid)
        {
            dataGrid.AutoGeneratingColumn += (s, e) =>
            {
                if (e.PropertyName == selectedPropertyName)
                {
                    var c = new DataGridTemplateColumn()
                    {
                        CellTemplate = (DataTemplate)dataGrid.Resources[cellTemplateKey],
                        Header = e.Column.Header,
                        HeaderTemplate = (DataTemplate)dataGrid.Resources[headerTemplateKey],
                        HeaderStringFormat = e.Column.HeaderStringFormat,
                        CanUserSort = false
                    };
                    e.Column = c;
                }
            };

            dataGrid.DataContext = this;
            dataGridBinding = dataGrid;
        }


        public bool? IsSelected
        {
            get
            {
                var list = Items.AsEnumerable().Select(row => row[selectedPropertyName]).Cast<bool>();

                bool? ret = false;
                if (list.Any())
                {
                    var uniqList = list.Distinct().ToList();
                    ret = (uniqList.Count() == 1) ? (bool?)uniqList.First() : null;
                }
                return ret;
            }
            set
            {
                Items.AsEnumerable().ToList().ForEach(row => row[selectedPropertyName] = value);
            }
        }


        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        }
    }
}
