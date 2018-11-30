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
        DataRow dataRow;
        public bool enable => dataRow.Field<bool>("Enable");
        public string ipStr => dataRow.Field<string>("IP");

        public IPParams(DataRow dataRow)
        {
            this.dataRow = dataRow;
        }
    }
}
