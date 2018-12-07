using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
