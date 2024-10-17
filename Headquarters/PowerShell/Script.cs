using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;

namespace Headquarters
{
    public class Script(string filepath)
    {
        #region static
        
        private const string PreProcessFunctionName = "PreProcess";
        private const string IpAddressProcessFunctionName = "IpAddressProcess";
        private const string PostProcessFunctionName = "PostProcess";

        private static readonly HashSet<string> ReservedFunctionNames = new(
            [
                PreProcessFunctionName,
                IpAddressProcessFunctionName,
                PostProcessFunctionName
            ],
            StringComparer.OrdinalIgnoreCase);
        
        public static Script Empty => new("");
        
        public static class ReservedParameterName
        {
            public const string Session = "session";
        }
        
        #endregion

        
        private readonly Dictionary<string, ScriptFunction> _scriptFunctionDictionary = [];
        
        
        public string Name { get; } = Path.GetFileNameWithoutExtension(filepath);
        
        public IEnumerable<string> ParameterNames => _scriptFunctionDictionary.Values.SelectMany(f => f.ParameterNames).Distinct();

        public bool HasPreProcess => _scriptFunctionDictionary.ContainsKey(PreProcessFunctionName);
        public bool HasPostProcess => _scriptFunctionDictionary.ContainsKey(PostProcessFunctionName);
        
        public ScriptFunction PreProcess => _scriptFunctionDictionary[PreProcessFunctionName];
        public ScriptFunction IpProcess => _scriptFunctionDictionary[IpAddressProcessFunctionName];
        public ScriptFunction PostProcess => _scriptFunctionDictionary[PostProcessFunctionName];


        public void Load() => ParseScript();

        private void ParseScript()
        {
            var ast = Parser.ParseFile(filepath, out _, out var errors);
            
            //TODO: error handling
            if (errors is {Length: > 0})
            {
                foreach (var e in errors)
                {
                    Console.WriteLine($@"{e}{Environment.NewLine}");
                }
                return;
            }
            
            var functionDefinitions = ast
                .FindAll(item => item is FunctionDefinitionAst, searchNestedScriptBlocks: false)
                .OfType<FunctionDefinitionAst>();
            
            _scriptFunctionDictionary.Clear();
            
            // 予約済み関数を取得
            foreach(var functionDefinition in functionDefinitions)
            {
                var name = functionDefinition.Name;
                if ( ReservedFunctionNames.Contains(functionDefinition.Name) )
                {
                    _scriptFunctionDictionary.Add(name, new ScriptFunction(functionDefinition));
                }
            }

            // 予約済み関数がない場合は、スクリプト全体として登録
            if (_scriptFunctionDictionary.Count == 0)
            {
                _scriptFunctionDictionary.Add(IpAddressProcessFunctionName, new ScriptFunction(Name, ast));
            }
        }
        
        

        public async void Run(IReadOnlyDictionary<string, object> parameters, IReadOnlyList<IpParameterSet> ipParamsList, int maxTaskNum, CancellationToken cancellationToken)
        {
            if (HasPreProcess)
            {
                var scriptResult = new ScriptResult()
                {
                    name = PreProcess.Name
                };
                
                var invokeParameter = new PowerShellRunner.InvokeParameter()
                {
                    parameters = parameters,
                    cancelToken = cancellationToken,
                    invocationStateChanged = (_, args) => scriptResult.Info = args.InvocationStateInfo
                };
                
                await PreProcess.Run(invokeParameter);
            }
        }

        public async Task<PowerShellRunner.Result> Run(string ipAddress, PowerShellRunner.InvokeParameter param)
        {
            return new PowerShellRunner.Result();
            // if (IsSessionRequired)
            // {
            //     param.parameters.TryGetValue(ParameterManager.SpecialParamName.UserName, out var userNameObject);
            //     param.parameters.TryGetValue(ParameterManager.SpecialParamName.UserPassword, out var userPasswordObject);
            //
            //     var userName = userNameObject as string ?? "";
            //     var userPassword = userPasswordObject as string ?? "";
            //
            //     var sessionResult = await SessionManager.CreateSession(ipAddress, userName, userPassword, param);
            //     var session = sessionResult.objs?.FirstOrDefault()?.BaseObject;
            //     if (session == null)
            //     {
            //         return sessionResult;
            //     }
            //     
            //     param.parameters = new Dictionary<string, object>(param.parameters)
            //     {
            //         { ReservedParameterName.Session, session }
            //     };
            // }
            //
            //
            // return await PowerShellRunner.InvokeAsync(_scriptString, param);
        }
    }
}
