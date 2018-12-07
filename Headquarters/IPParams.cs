using System.Collections.Generic;
using System.Data;

namespace Headquarters
{
    class IPParams
    {
        public static string isSelectedPropetyName = "IsSelected";

        DataRow dataRow;
        public bool isSelected => dataRow.Field<bool>(isSelectedPropetyName);
        public string ipStr => dataRow.Field<string>("IP");


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
