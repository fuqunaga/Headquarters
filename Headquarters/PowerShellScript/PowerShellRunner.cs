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
        private static readonly string AddModulePathString = $$"""
                                                               $path = "{{Environment.CurrentDirectory}}\Scripts\Modules"
                                                               if ( $env:PSModulePath.EndsWith($path) -eq $false )
                                                               {
                                                                   $env:PSModulePath += ";$path"
                                                               }
                                                               """;

        /// <summary>
        /// Headquarters.Path 属性を定義
        /// 「生成された型は、パブリック メソッドまたはパブリック プロパティを定義していません。」の警告を抑制するために WarningPreference を一時的に変更している
        /// 3>$null みたいなリダイレクトだと警告が抑制できなかった
        /// </summary>
        private const string AddAttributeString = $$"""
                                                    $tmpWarningPreference = $WarningPreference
                                                    $WarningPreference = 'SilentlyContinue'

                                                    Add-Type @"
                                                        using System;
                                                        namespace {{CustomAttributeName.NamespaceName}}
                                                        {
                                                            public class {{CustomAttributeName.Path}}Attribute : Attribute{}
                                                        }
                                                    "@

                                                    $WarningPreference = $tmpWarningPreference 
                                                    """;
            
        public readonly struct InvokeParameter(
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken,
            PowerShellEventSubscriber eventSubscriber)
        {
            public Dictionary<string, object> Parameters => parameters;
            public CancellationToken CancellationToken => cancellationToken;
            public PowerShellEventSubscriber EventSubscriber => eventSubscriber;
        }

        public class Result
        {
            public bool canceled;
            public bool hasError;
            public Collection<PSObject>? objs;

            public bool IsSucceed => !canceled && !hasError;
        }
        
        public static async Task<Result> InvokeAsync(string scriptString, InvokeParameter param)
            => await InvokeAsync(scriptString, null, param);

        public static async Task<Result> InvokeAsync(string scriptString, string? commandName, InvokeParameter param)
        {
            using var powerShell = PowerShell.Create();
            
            powerShell.AddScript(AddModulePathString);
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
            
            var output = new PSDataCollection<PSObject>();
            param.EventSubscriber.onOutputAdded += obj =>
            {
                result.objs ??= [];
                result.objs.Add(obj);
            };
            
            param.EventSubscriber.Subscribe(powerShell, output);

            try
            {
                await Task.Run(() =>
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
                    
                    powerShell.Invoke<PSObject, PSObject>(null, output, null);
                    
                }, param.CancellationToken);
            }
            catch (Exception e)
            {
                // キャンセルの例外は無視
                if (e is not OperationCanceledException)
                {
                    powerShell.Streams.Error.Add(new ErrorRecord(e, "Invoke", ErrorCategory.NotSpecified, null));
                    result.hasError = true;
                }
            }

            result.hasError |= powerShell.HadErrors;
            
            return result;
        }
    }
}
