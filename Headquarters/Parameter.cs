using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Headquarters
{
    class Parameter
    {
        public string Name { get; set; }
        public string Value
        {
            get { return ParameterManager.Instance.Get(Name); }
            set
            {
                ParameterManager.Instance.Set(Name, value);
            }
        }

        public Parameter(string name)
        {
            this.Name = name;
        }
    }
}
