using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace Headquarters
{
    public static class PowerShellRunner
    {
        public class InvokeParameter
        {
            public required IReadOnlyDictionary<string, object> parameters;
            public CancellationToken cancelToken;
            public required EventHandler<PSInvocationStateChangedEventArgs> invocationStateChanged;
        }

        public class Result
        {
            public bool canceled;
            public PSDataCollection<PSObject>? objs;
            public List<ErrorRecord>? errors;

            public bool IsSucceed => !canceled &&
                                     (errors == null || errors.Count == 0);
        }
        

        public static async Task<Result> InvokeAsync(string scriptString, InvokeParameter param)
        {
            using var powerShell = PowerShell.Create();
            
            powerShell.InvocationStateChanged += param.invocationStateChanged;

            powerShell.AddScript(scriptString);
            powerShell.AddParameters((IDictionary)param.parameters);

            var result = new Result();
            try
            {
                await using var _ = param.cancelToken.Register(state =>
                    {
                        if (state is PowerShell ps)
                        {
                            ps.Stop();
                        }

                        result.canceled = true;
                    },
                    powerShell
                );


                result.objs = await powerShell.InvokeAsync();
                
                result.errors = powerShell.Streams.Error.ToList();
            }
            catch (Exception e)
            {
                result.errors = (new[] { new ErrorRecord(e, "", ErrorCategory.InvalidData, null) }).ToList();
            }

            return result;
        }
    }
}
