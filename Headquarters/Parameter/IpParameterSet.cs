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

        public bool IsSelected => dataRow.Field<bool>(IsSelectedPropertyName);
        public string IpString => dataRow.Field<string>(IpPropertyName) ?? string.Empty;


        public string? Get(string name)
        {
            return dataRow.Table.Columns.Contains(name) ? dataRow.Field<string>(name) : null;
        }
    }
}
