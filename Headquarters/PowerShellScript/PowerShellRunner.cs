using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace Headquarters
{
    public static class PowerShellRunner
    {
        private const string AddAttributeString = $$"""
                                                   Add-Type @"
                                                       using System;
                                                       namespace {{CustomAttributeName.NamespaceName}}
                                                       {
                                                           public class {{CustomAttributeName.Path}}Attribute : Attribute
                                                           {}
                                                       }
                                                   "@
                                                   """;

        public readonly struct InvokeParameter(
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken,
            RunspacePool runspacePool,
            EventHandler<PSInvocationStateChangedEventArgs> invocationStateChanged)
        {
            public Dictionary<string, object> Parameters => parameters;
            public CancellationToken CancellationToken => cancellationToken;
            public RunspacePool RunspacePool => runspacePool;
            public EventHandler<PSInvocationStateChangedEventArgs> InvocationStateChanged => invocationStateChanged;
        }

        public class Result
        {
            public bool canceled;
            public Collection<PSObject>? objs;
            public List<ErrorRecord>? errors;

            public bool IsSucceed => !canceled && !HasError;
            public bool HasError => errors is { Count: > 0 };
        }
        
        public static async Task<Result> InvokeAsync(string scriptString, InvokeParameter param)
            => await InvokeAsync(scriptString, null, param);

        public static async Task<Result> InvokeAsync(string scriptString, string? commandName, InvokeParameter param)
        {
            using var powerShell = PowerShell.Create();
            
            powerShell.InvocationStateChanged += param.InvocationStateChanged;
            powerShell.RunspacePool = param.RunspacePool;
            
            
            powerShell.AddScript(AddAttributeString);
            powerShell.AddStatement();
            powerShell.AddScript(scriptString);
            if (commandName != null)
            {
                powerShell.AddStatement();
                powerShell.AddCommand(commandName);
            }
            
            powerShell.AddParameters(param.Parameters);


            var result = new Result();

            try
            {
                result.objs = await Task.Run(() =>
                {  
                    using var _ = param.CancellationToken.Register(
                        state => {
                            if (state is PowerShell ps)
                            {
                                ps.Stop();
                            }

                            result.canceled = true;
                        },
                        powerShell
                    );
                    
                    return powerShell.Invoke();
                }, param.CancellationToken);
            }
            catch (Exception e)
            {
                // キャンセルの例外は無視
                if (e is not OperationCanceledException)
                {
                    result.errors = [new ErrorRecord(e, "Invoke", ErrorCategory.NotSpecified, null)];
                }
            }

            result.errors ??= [];
            result.errors.AddRange(powerShell.Streams.Error);
            
            return result;
        }
    }
}
