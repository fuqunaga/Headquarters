using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                        //                        SortMemberPath = e.PropertyName // this is used to index into the DataRowView so it MUST be the property's name (for this implementation anyways)
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
                var uniqueList = Items.AsEnumerable().Select(row => row[selectedPropertyName]).Distinct().ToList();
                return uniqueList.Count() == 1 ? (bool?)uniqueList.First() : null;
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
