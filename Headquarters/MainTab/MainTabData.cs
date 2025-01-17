using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Headquarters;

/// <summary>
/// MainTabに相当するデータクラス
/// JsonSerializer対応
/// </summary>
public struct MainTabData
{
    public string Name { get; set; } = "";
    public bool IsLocked { get; set; }
    public List<Dictionary<string, string>> IpList { get; set; }
    
    public ScriptChainData ScriptChainData { get; set; }
    
    public MainTabData()
    {
        IpList = [];
        ScriptChainData = new ScriptChainData();
    }

    public MainTabData(DataTable dataTable, ScriptChainData scriptChainData)
    {
        IpList = CreateIpList(dataTable);
        ScriptChainData = scriptChainData;
    }

    public DataTable CreateIpListDataTable()
    {
        var dataTable = new DataTable();
        var columns = dataTable.Columns;
        
        foreach (var rowDictionary in IpList)
        {
            var row = dataTable.NewRow();
            foreach (var pair in rowDictionary)
            {
                var key = pair.Key;
                var stringValue = pair.Value;
                
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
        return dataTable.Rows.Cast<DataRow>().Select(
            row => dataTable.Columns.Cast<DataColumn>().ToDictionary(
                column => column.ColumnName,
                column => row[column].ToString()
            )).ToList();

    }
}