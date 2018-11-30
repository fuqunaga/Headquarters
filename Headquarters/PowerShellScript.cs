using System.Management.Automation;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Management.Automation.Runspaces;

namespace Headquarters
{
    public class PowerShellScript
    {
        public class Result
        {
            public Collection<PSObject> objs;
            public List<ErrorRecord> errors;
        }

        public readonly string name;
        public readonly string script;

        public PowerShellScript(string name, string script)
        {
            this.name = name;
            this.script = script;
        }

        public Result Invoke(Runspace rs, IDictionary param)
        {
            Result ret;
            using (var ps = PowerShell.Create())
            {
                ps.Runspace = rs;

                ps.AddScript(script);
                ps.AddParameters(param);

                ret = new Result()
                {
                    objs = ps.Invoke(),
                    errors = ps.Streams.Error.ToList()
                };
            }

            return ret;
        }
    }
}
