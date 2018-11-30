using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Headquarters
{
    class ScriptViewModel
    {
        public string Header => script.name;
        public List<Parameter> Parameters { get; protected set; }


        public Script script { get; protected set; }

        public ScriptViewModel(Script script)
        {
            this.script = script;

            Parameters = script.paramNames.Select(p => new Parameter(p)).ToList();
        }
    }
}
