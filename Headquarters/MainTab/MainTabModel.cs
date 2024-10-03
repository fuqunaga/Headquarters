using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Headquarters;

/// <summary>
/// MainTabに相当する処理を行うクラス
/// </summary>
public class MainTabModel
{
    // Data Format for JSON
    public struct MainTabData
    {
        public string TabHeader { get; init; }
        public List<Dictionary<string, string?>> IpList { get; init; }
    }
    
    public string Header { get; set; } = string.Empty;
    public DataTable IpListDataTable { get; set; } = new();

    public MainTabModel()
    {
    }

    public MainTabModel(MainTabData data)
    {
        Deserialize(data);
    }
    
    public MainTabData Serialize()
    {
        var ipList = IpListDataTable.AsEnumerable().Select(
            row => IpListDataTable.Columns.Cast<DataColumn>().ToDictionary(
                column => column.ColumnName,
                column => row[column].ToString()
            )).ToList();
        
        return new MainTabData()
        {
            TabHeader = Header,
            IpList = ipList
        };
    }
    

    public void Deserialize(MainTabData data)
    {
        Header = data.TabHeader;
        
        IpListDataTable.Clear();
        var columns = IpListDataTable.Columns;
        foreach (var rowDictionary in data.IpList)
        {
            var row = IpListDataTable.NewRow();
            foreach (var (key, value) in rowDictionary)
            {
                if (!columns.Contains(key))
                {
                    columns.Add(key);
                }
                
                row[key] = value;
            }
            IpListDataTable.Rows.Add(row);
        }
        
        if (IpListDataTable.Columns[IPParams.isSelectedPropertyName] == null)
        {
            IpListDataTable.Columns.Add(IPParams.isSelectedPropertyName, typeof(bool)).SetOrdinal(0);
        }
        
        if (IpListDataTable.Columns[IPParams.ipPropertyName] == null)
        {
            IpListDataTable.Columns.Add(IPParams.ipPropertyName, typeof(string)).SetOrdinal(1);
        }
    }
}