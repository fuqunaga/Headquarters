using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace Headquarters
{
    public class PowerShellScript(string script)
    {
        public class InvokeParameter
        {
            // public RunspacePool rsp;
            public required Dictionary<string, object> parameters;
            public CancellationToken cancelToken;
            public required EventHandler<PSInvocationStateChangedEventArgs> invocationStateChanged;

            public InvokeParameter()
            {
            }
            
            [SetsRequiredMembers]
            public InvokeParameter(InvokeParameter other)
            {
                // rsp = other.rsp;
                parameters = other.parameters;
                cancelToken = other.cancelToken;
                invocationStateChanged = other.invocationStateChanged;
            }
        }

        public class Result
        {
            public bool canceled;
            public Collection<PSObject>? objs;
            public List<ErrorRecord>? errors;

            public bool IsSucceed => !canceled &&
                                     (errors == null || errors.Count == 0);
        }
        

        public Result Invoke(InvokeParameter param)
        {
            using var ps = PowerShell.Create();
            
            ps.InvocationStateChanged += param.invocationStateChanged;
            // ps.RunspacePool = param.rsp;

            ps.AddScript(script);
            ps.AddParameters(param.parameters);

            var ret = new Result();
            try
            {
                using (param.cancelToken.Register(() => { ps.Stop(); ret.canceled = true; }))
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
