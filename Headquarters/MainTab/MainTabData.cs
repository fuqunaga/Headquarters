using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Headquarters;

/// <summary>
/// MainTabに相当するデータクラス
/// JsonSerializer対応
/// </summary>
public readonly struct MainTabData
{
    public string TabHeader { get;  init ; } = string.Empty;
    public List<Dictionary<string, string?>> IpList { get; init; }

    public MainTabData()
    {
        IpList = new();
    }

    public MainTabData(DataTable dataTable)
    {
        IpList = CreateIpList(dataTable);
    }

    public DataTable CreateIpListDataTable()
    {
        var dataTable = new DataTable();
        var columns = dataTable.Columns;
        
        foreach (var rowDictionary in IpList)
        {
            var row = dataTable.NewRow();
            foreach (var (key, value) in rowDictionary)
            {
                var isSelected = (key == IPParams.isSelectedPropertyName);
                
                if (!columns.Contains(key))
                {
                    columns.Add(key, isSelected ? typeof(bool) : typeof(string));
                }


                row[key] = isSelected
                    ? value != null && bool.Parse(value)
                    : value;
            }
            
            dataTable.Rows.Add(row);
        }

        if (dataTable.Columns[IPParams.ipPropertyName] == null)
        {
            dataTable.Columns.Add(IPParams.ipPropertyName, typeof(string)).SetOrdinal(0);
        }

        return dataTable;
    }
    
    public List<Dictionary<string, string?>> CreateIpList(DataTable dataTable)
    {
        return dataTable.AsEnumerable().Select(
            row => dataTable.Columns.Cast<DataColumn>().ToDictionary(
                column => column.ColumnName,
                column => row[column].ToString()
            )).ToList();

    }
}