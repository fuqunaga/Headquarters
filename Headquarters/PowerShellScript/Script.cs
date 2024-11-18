﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;

namespace Headquarters
{
    public class Script(string filepath)
    {
        #region static
        
        private const string PreProcessFunctionName = "PreProcess";
        private const string IpAddressProcessFunctionName = "IpAddressProcess";
        private const string PostProcessFunctionName = "PostProcess";

        private static readonly IReadOnlyCollection<string> ReservedFunctionNames = new HashSet<string>(
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
            public const string Ip = "ip";
            public const string Credential = "credential";
        }
        
        public static readonly IReadOnlyList<string> ReservedParameterNames =
        [
            ReservedParameterName.Session,
            ReservedParameterName.Ip,
            ReservedParameterName.Credential
        ];
        
        #endregion

        public event Action? onUpdate;
        
        private readonly Dictionary<string, ScriptFunction> _scriptFunctionDictionary = [];
        private CommentHelpInfo? _helpInfo;
        
        public string  FilePath => filepath;

        public string Name { get; } = Path.GetFileNameWithoutExtension(filepath);
        public bool HasParseError => ParseErrorMessages.Count > 0;
        public string Synopsis => _helpInfo?.Synopsis?.TrimEnd('\r', '\n') ?? "";
        public string Description => _helpInfo?.Description?.TrimEnd('\r', '\n') ?? "";
        
        public List<string> ParseErrorMessages { get; private set; } = [];        
        public IEnumerable<string> EditableParameterNames => _scriptFunctionDictionary.Values.SelectMany(f => f.ParameterNames).Distinct().Except(ReservedParameterNames);

        public bool HasPreProcess => _scriptFunctionDictionary.ContainsKey(PreProcessFunctionName);
        public bool HasPostProcess => _scriptFunctionDictionary.ContainsKey(PostProcessFunctionName);
        
        public ScriptFunction PreProcess => _scriptFunctionDictionary[PreProcessFunctionName];
        public ScriptFunction IpAddressProcess => _scriptFunctionDictionary[IpAddressProcessFunctionName];
        public ScriptFunction PostProcess => _scriptFunctionDictionary[PostProcessFunctionName];
        
        public string GetParameterHelp(string parameterName)
        {
            if ( _helpInfo?.Parameters.TryGetValue(parameterName.ToUpper(), out var help) ?? false)
            {
                return help.TrimEnd('\r', '\n');
            }

            return string.Empty;
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
            
            if (!File.Exists(filepath))
            {
                // ファイルが見つからないときのParser.ParseFile()のParseErrorの文言が変なので自前で作成
                ParseErrorMessages.Add($"ファイル[{filepath}]が見つかりませんでした");
                return;
            }

            var ast = Parser.ParseFile(filepath, out _, out var parseErrors);
            
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
                _scriptFunctionDictionary.Add(IpAddressProcessFunctionName, new ScriptFunction(Name, ast));
            }
            
            _helpInfo = ast.GetHelpContent();

            if (parseErrors != null)
            {
                ParseErrorMessages.AddRange(parseErrors.Select(e => e.ToString()));
            }
        }
    }
}
