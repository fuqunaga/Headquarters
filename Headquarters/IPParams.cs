using System.Collections.Generic;
using System.Data;

namespace Headquarters
{
    public class IPParams
    {
        public static string isSelectedPropertyName = "IsSelected";
        public static string ipPropertyName = "IP";

        DataRow dataRow;
        public bool isSelected => dataRow.Field<bool>(isSelectedPropertyName);
        public string ipStr => dataRow.Field<string>(ipPropertyName);


        public IPParams(DataRow dataRow)
        {
            this.dataRow = dataRow;
        }

        public string Get(string name)
        {
            return dataRow.Table.Columns.Contains(name) ? dataRow.Field<string>(name) : null;
        }
    }
}
