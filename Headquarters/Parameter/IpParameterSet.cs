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
        public string IpString => dataRow[IpPropertyName] as string ?? "";


        public string? Get(string name)
        {
            // カラムが無ければnull
            if (!dataRow.Table.Columns.Contains(name))
            {
                return null;
            }
            
            // カラムがあるが値が無い場合は空文字
            // nullを返さずIPListのパラメータを参照してもらう
            if (dataRow.IsNull(name))
            {
                return "";
            }

            return dataRow[name] as string;
        }
    }
}
