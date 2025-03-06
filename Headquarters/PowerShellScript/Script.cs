using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;

namespace Headquarters
{
    public class Script
    {
        #region Static
        
        public static class ReservedParameterName
        {
            public const string Session = "session";
            public const string Ip = "ip";
            public const string TaskContext = "taskContext";
        }
        
        private const string BeginTaskFunctionName = "BeginTask";
        private const string IpAddressTaskFunctionName = "IpAddressTask";
        private const string EndTaskFunctionName = "EndTask";

        public static Script Empty => new("");
        
        private static readonly IReadOnlyCollection<string> ReservedFunctionNames = new HashSet<string>(
            [
                BeginTaskFunctionName,
                IpAddressTaskFunctionName,
                EndTaskFunctionName
            ],
            StringComparer.OrdinalIgnoreCase);
        
        public static readonly IReadOnlyList<string> ReservedParameterNames =
        [
            ReservedParameterName.Session,
            ReservedParameterName.Ip,
            ReservedParameterName.TaskContext
        ];
        
        #endregion

        public event Action? onUpdate;

        private readonly Dictionary<string, ScriptFunction> _scriptFunctionDictionary = [];
        private CommentHelpInfo? _helpInfo;
        
        public string  FilePath { get; }

        public string Name { get; }
        public bool HasParseError => ParseErrorMessages.Count > 0;
        public string Synopsis => _helpInfo?.Synopsis?.TrimEnd('\r', '\n') ?? "";
        public string Description => _helpInfo?.Description?.TrimEnd('\r', '\n') ?? "";
        
        public List<string> ParseErrorMessages { get; } = [];
        
        // 変更可能なパラメータ
        // 複数の関数に同じパラメータがあった場合とりあえず最初のものを採用
        // HelpInfoがある場合のその順番にする
        public IEnumerable<ScriptParameterDefinition> EditableScriptParameterDefinitions =>
            FilterEditableScriptParameterDefinitions(_scriptFunctionDictionary.Values.SelectMany(f => f.Parameters));

        public IEnumerable<ScriptParameterDefinition> EditableScriptParameterDefinitionsOnlyIpAddressTask
        {
            get
            {
                var parameterDefinitions = IpAddressTask.Parameters.AsEnumerable();
                if (HasBeginTask)
                {
                    parameterDefinitions = parameterDefinitions.Except(BeginTask.Parameters, ScriptParameterDefinition.NameEqualityComparer.Default);
                }

                if (HasEndTask)
                {
                    parameterDefinitions = parameterDefinitions.Except(EndTask.Parameters, ScriptParameterDefinition.NameEqualityComparer.Default);
                }
                    
                return FilterEditableScriptParameterDefinitions(parameterDefinitions);
            }
        }

        public bool HasBeginTask => _scriptFunctionDictionary.ContainsKey(BeginTaskFunctionName);
        public bool HasIpAddressTask => _scriptFunctionDictionary.ContainsKey(IpAddressTaskFunctionName);
        public bool HasEndTask => _scriptFunctionDictionary.ContainsKey(EndTaskFunctionName);
        
        public ScriptFunction BeginTask => _scriptFunctionDictionary[BeginTaskFunctionName];
        public ScriptFunction IpAddressTask => _scriptFunctionDictionary[IpAddressTaskFunctionName];
        public ScriptFunction EndTask => _scriptFunctionDictionary[EndTaskFunctionName];


        public Script(string filePath)
        {
            FilePath = filePath;
            Name = Path.GetFileNameWithoutExtension(FilePath);
            Update();
        }
        
        public string GetParameterHelp(string parameterName)
        {
            if ( _helpInfo?.Parameters.TryGetValue(parameterName.ToUpper(), out var help) ?? false)
            {
                return help.TrimEnd('\r', '\n');
            }

            return string.Empty;
        }

        // 変更可能なパラメータ
        // HelpInfoがある場合のその順番にする
        private IEnumerable<ScriptParameterDefinition> FilterEditableScriptParameterDefinitions(IEnumerable<ScriptParameterDefinition> parameterDefinitions)
        {
            var editableParameters = parameterDefinitions
                .GroupBy(p => p.Name)
                .Select(group => group.First())
                .Where(p => !ReservedParameterNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();
                
            if (_helpInfo == null)
            {
                return editableParameters;
            }

            var helpParameters = _helpInfo.Parameters.Keys
                .Select(name => editableParameters.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .Where(p => p != null)
                .Select(p => p!)
                .ToList();

            return helpParameters.Concat(editableParameters.Except(helpParameters));
        }

        public void Update()
        {
            ParseScript();
            onUpdate?.Invoke();
        }

        private void ParseScript()
        {
            _scriptFunctionDictionary.Clear();
            _helpInfo = null;
            ParseErrorMessages.Clear();
            
            if (!File.Exists(FilePath))
            {
                // ファイルが見つからないときのParser.ParseFile()のParseErrorの文言が変なので自前で作成
                ParseErrorMessages.Add($"ファイル[{FilePath}]が見つかりませんでした");
                return;
            }

            var ast = Parser.ParseFile(FilePath, out _, out var parseErrors);
            
            var functionDefinitions = ast
                .FindAll(item => item is FunctionDefinitionAst, searchNestedScriptBlocks: false)
                .OfType<FunctionDefinitionAst>();
            
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
                _scriptFunctionDictionary.Add(IpAddressTaskFunctionName, new ScriptFunction(Name, ast));
            }
            
            _helpInfo = ast.GetHelpContent();

            if (parseErrors != null)
            {
                ParseErrorMessages.AddRange(parseErrors.Select(e => e.ToString()));
            }
        }
    }
}
