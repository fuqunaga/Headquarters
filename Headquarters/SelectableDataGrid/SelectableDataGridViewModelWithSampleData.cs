namespace Headquarters;

public class SelectableDataGridViewModelWithSampleData : SelectableDataGridViewModel
{    
    public SelectableDataGridViewModelWithSampleData()
    {
        Items.Columns.Add("Column0", typeof(string));
        Items.Columns.Add("Column1", typeof(string));
        
        Items.Rows.Add(true, "Row0-0", "Row0-1");
        Items.Rows.Add(true, "Row1-0", "Row1-1");
        Items.Rows.Add(true, "Row2-0", "Row2-1");
    }
}