using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Headquarters
{
    public partial class Script
    {
        #region static
        
        public static Script Empty => new("");
        
        public static class ReservedParameterName
        {
            public const string Session = "session";
        }
        
        [GeneratedRegex(@"(?<=param\().*?(?=\))")]
        private static partial Regex ParamRegex();
        
        #endregion


        private readonly string _filepath;
        private string _scriptString = "";
        
        public string Name { get; }

        public List<string> ParamNames { get; private set; } = [];


        public Script(string filepath)
        {
            _filepath = filepath;
            Name = Path.GetFileNameWithoutExtension(filepath);
            Load();
        }

        public void Load()
        {
            if (!File.Exists(_filepath)) return;
            
            _scriptString = File.ReadAllText(_filepath);
            ParamNames = SearchParameters(_scriptString);
        }

        private static List<string> SearchParameters(string scriptString)
        {
            var match = ParamRegex().Match(scriptString);
            return match.Value
                .Replace("$", "")
                .Replace(" ", "")
                .Split(',')
                .Where(str => string.Compare(str, ReservedParameterName.Session, StringComparison.OrdinalIgnoreCase) != 0)
                .ToList();
        }

        public async Task<PowerShellRunner.Result> Run(string ipAddress, PowerShellRunner.InvokeParameter param)
        {
            param.parameters.TryGetValue(ParameterManager.SpecialParamName.UserName, out var userNameObject);
            param.parameters.TryGetValue(ParameterManager.SpecialParamName.UserPassword, out var userPasswordObject);
            
            var userName = userNameObject as string ?? "";
            var userPassword = userPasswordObject as string ?? "";

            var sessionResult = await SessionManager.CreateSession(ipAddress, userName, userPassword, param);
            var session = sessionResult.objs?.FirstOrDefault()?.BaseObject;
            if (session == null)
            {
                return sessionResult;
            }

            param.parameters = new Dictionary<string, object>(param.parameters)
            {
                { ReservedParameterName.Session, session }
            };
            
            return await PowerShellRunner.InvokeAsync(_scriptString, param);
        }
    }
}
