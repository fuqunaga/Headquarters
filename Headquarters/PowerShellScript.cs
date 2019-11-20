using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Collections;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Collections.Generic;
using System;

namespace Headquarters
{
    public class PowerShellScript
    {
        public class Result
        {
            public bool canceled;
            public Collection<PSObject> objs;
            public List<ErrorRecord> errors;

            public bool IsSuccessed => !canceled && !errors.Any();
        }

        public readonly string name;
        public readonly string script;

        public PowerShellScript(string name, string script)
        {
            this.name = name;
            this.script = script;
        }

        public Result Invoke(RunspacePool rsp, IDictionary param, CancellationToken cancelToken)
        {
            Result ret;
            using (var ps = PowerShell.Create())
            {
                ps.RunspacePool = rsp;

                ps.AddScript(script);
                ps.AddParameters(param);

                ret = new Result();
                try
                {
                    using (cancelToken.Register(() => { ps.Stop(); ret.canceled = true; }))
                    {
                        ret.objs = ps.Invoke();
                    }
                    ret.errors = ps.Streams.Error.ToList();
                }
                catch (Exception e)
                {
                    ret.errors = (new[] { new ErrorRecord(e, "", ErrorCategory.InvalidData, null) }).ToList();
                }

                return ret;
            }
        }
    }
}
