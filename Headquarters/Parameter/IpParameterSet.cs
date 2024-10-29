using System.Data;

namespace Headquarters
{
    /// <summary>
    /// IP アドレスごとのPowerShell用パラメータ
    /// IP Listの一行に相当する
    /// </summary>
    public class IpParameterSet(DataRow dataRow)
    {
        public static string IsSelectedPropertyName  => SelectableDataGridViewModel.SelectedPropertyName;
        public const string IpPropertyName = "IP";

        public bool IsSelected => (bool)dataRow[IsSelectedPropertyName];
        public string IpString => (string)dataRow[IpPropertyName];


        public string? Get(string name)
        {
            return dataRow.Table.Columns.Contains(name) ? (string)dataRow[name] : null;
        }
    }
}
