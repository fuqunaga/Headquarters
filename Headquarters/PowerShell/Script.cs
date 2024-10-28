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
        public ScriptFunction IpAddressProcess => _scriptFunctionDictionary[IpAddressProcessFunctionName];
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
    }
}
