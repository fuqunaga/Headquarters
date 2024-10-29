using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            public Dictionary<string, object> parameters;
            public CancellationToken cancellationToken;
            public EventHandler<PSInvocationStateChangedEventArgs> invocationStateChanged;
        }

        public class Result
        {
            public bool canceled;
            public Collection<PSObject>? objs;
            public List<ErrorRecord>? errors;

            public bool IsSucceed => !canceled &&
                                     (errors == null || errors.Count == 0);
        }
        
        public static async Task<Result> InvokeAsync(string scriptString,InvokeParameter param)
            => await InvokeAsync(scriptString, null, param);

        public static async Task<Result> InvokeAsync(string scriptString, string? commandName, InvokeParameter param)
        {
            using var powerShell = PowerShell.Create();
            
            powerShell.InvocationStateChanged += param.invocationStateChanged;

            
            powerShell.AddScript(scriptString, false);
            if (commandName != null)
            {
                powerShell.AddStatement();
                powerShell.AddCommand(commandName);
            }
            
            powerShell.AddParameters((IDictionary)param.parameters);


            var result = new Result();

            using var _ = param.cancellationToken.Register(state =>
                {
                    if (state is PowerShell ps)
                    {
                        ps.Stop();
                    }

                    result.canceled = true;
                },
                powerShell
            );


            try
            {
                result.objs = await Task.Run(() => powerShell.Invoke());
            }
            catch (Exception e)
            {
                result.errors = [new ErrorRecord(e, "InvokeAsync", ErrorCategory.NotSpecified, null)];
            }

            result.errors ??= [];
            result.errors.AddRange(powerShell.Streams.Error);
            
            return result;
        }
    }
}
