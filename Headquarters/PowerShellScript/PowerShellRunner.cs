using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace Headquarters
{
    public static class PowerShellRunner
    {
        private static readonly string AddModulePathString = $$"""
                                                               $paths = @(
                                                                   "{{Path.Combine(PathSetting.DataPath, "Modules")}}"
                                                                   "{{Path.Combine(Profile.ScriptsFolderPath, "Modules")}}"
                                                               )
                                                               
                                                               $currentPaths = $env:PSModulePath -split ";"
                                                               foreach ($path in $paths) {
                                                                   if ($currentPaths -notcontains $path) {
                                                                       $env:PSModulePath = "$env:PSModulePath;$path"
                                                                   }
                                                               }
                                                               """;
        
        private static readonly string AddChangeExecutionPolicyString = "Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process -Force";

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
            
        public struct InvokeParameter(
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken,
            PowerShellEventSubscriber eventSubscriber)
        {
            public Dictionary<string, object> Parameters => parameters;
            public CancellationToken CancellationToken => cancellationToken;
            public PowerShellEventSubscriber EventSubscriber => eventSubscriber;
            
            // Sessionを使用する場合、作成したRunspace内のみで有効っぽいので
            // Session使用時のみあらかじめRunspaceを作成しスクリプト実行時に再利用する
            public Runspace? Runspace { get; set; }
        }

        public class Result
        {
            public bool canceled;
            public bool hasError;
            public Collection<PSObject>? objs;

            public bool IsSucceed => !canceled && !hasError;
        }
        
        public static async Task<Result> InvokeAsync(string scriptString, InvokeParameter param, string? commandName = null)
        {
            using var powerShell = PowerShell.Create();
            
            // 事前にPSSessionを作成してスクリプトに渡す場合はRunspaceを共有していないと使えないっぽいので
            // PSSessionがある場合のみRunspaceを事前に作成して再利用する
            if(param.Runspace != null)
            {
                // ReSharper disable once MethodHasAsyncOverload
                param.Runspace.Open();
                powerShell.Runspace = param.Runspace;
            }
            
            powerShell.AddScript(AddModulePathString);
            powerShell.AddScript(AddChangeExecutionPolicyString);
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
            
            
            param.EventSubscriber.onOutputAdded += obj =>
            {
                result.objs ??= [];
                result.objs.Add(obj);
            };
            
            // エラー出力時は停止する
            // PowerShellのエラーはWrite-Errorのように停止しないエラーがある
            // エラーが出てる場合、それ以降の処理が無駄になる場合が多いので停止する
            // $ErrorActionPreference = "Stop" はエラーメッセージに余計な文言がついて分かりにくいのでやめた
            param.EventSubscriber.onErrorAdded += _ =>
            {
                // powerShell.Stop()だと処理が返ってこないので非同期で停止
                powerShell.BeginStop(null, null);
                result.hasError = true;
            };
            
            var output = new PSDataCollection<PSObject>();
            param.EventSubscriber.Subscribe(powerShell, output);
            
            
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

            try
            {
                await Task.Run(() =>
                {  
                    powerShell.Invoke<PSObject, PSObject>(null, output, null);
                }, param.CancellationToken);
            }
            catch (Exception e)
            {
                powerShell.Streams.Error.Add(new ErrorRecord(e, "Invoke", ErrorCategory.NotSpecified, null));
            }

            // エラーイベントをListenしてるからいらない？
            result.hasError |= powerShell.HadErrors;
            
            return result;
        }
    }
}
