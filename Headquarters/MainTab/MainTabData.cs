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
    public List<Dictionary<string, string>> IpList { get; init; }
    public Dictionary<string, Dictionary<string, string>> TabParameterDictionary { get; init; }

    
    public MainTabData()
    {
        IpList = [];
        TabParameterDictionary = new Dictionary<string, Dictionary<string, string>>();
    }

    public MainTabData(DataTable dataTable, TabParameterSet tabParameterSet)
    {
        IpList = CreateIpList(dataTable);
        TabParameterDictionary = tabParameterSet.ScriptParameterSetTable;
    }

    public DataTable CreateIpListDataTable()
    {
        var dataTable = new DataTable();
        var columns = dataTable.Columns;
        
        foreach (var rowDictionary in IpList)
        {
            var row = dataTable.NewRow();
            foreach (var (key, stringValue) in rowDictionary)
            {
                var isSelected = (key == IpParameterSet.IsSelectedPropertyName);
                object value = isSelected 
                    ?  (bool.TryParse(stringValue, out var v) && v) 
                    : stringValue;
                
                
                if (!columns.Contains(key))
                {
                    columns.Add(key, value.GetType());
                }

                row[key] = value;
            }
            
            dataTable.Rows.Add(row);
        }

        if (dataTable.Columns[IpParameterSet.IpPropertyName] == null)
        {
            dataTable.Columns.Add(IpParameterSet.IpPropertyName, typeof(string)).SetOrdinal(0);
        }

        return dataTable;
    }

    private static List<Dictionary<string, string>> CreateIpList(DataTable dataTable)
    {
        return dataTable.AsEnumerable().Select(
            row => dataTable.Columns.Cast<DataColumn>().ToDictionary(
                column => column.ColumnName,
                column => row[column].ToString() ?? string.Empty
            )).ToList();

    }

    public TabParameterSet CreateTabParameterSet() => new(TabParameterDictionary);
    
}