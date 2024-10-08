using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Headquarters
{
    public partial class Script
    {
        public static Script Empty => new("");
        
        public static class ReservedParameterName
        {
            public const string Session = "session";
        }
        
        
        [GeneratedRegex(@"(?<=param\().*?(?=\))")]
        private static partial Regex ParamRegex();

        

        public string Name { get; }

        public List<string> paramNames { get; protected set; }

        readonly string filepath;
        PowerShellScript psScript;

        public Script(string filepath)
        {
            this.filepath = filepath;
            Name = Path.GetFileNameWithoutExtension(filepath);
            Load();
        }

        public void Load()
        {
            if (!File.Exists(filepath)) return;
            
            var script = File.ReadAllText(filepath);
            psScript = new PowerShellScript(Name, script);
            paramNames = SearchParameters(script);
        }

        private static List<string> SearchParameters(string script)
        {
            var match = ParamRegex().Match(script);
            return match.Value
                .Replace("$", "")
                .Replace(" ", "")
                .Split(',')
                .Where(str => string.Compare(str, ReservedParameterName.Session, StringComparison.OrdinalIgnoreCase) != 0)
                .ToList();
        }

        public PowerShellScript.Result Run(string ipAddress, PowerShellScript.InvokeParameter param)
        {
            PowerShellScript.Result result;

            param.parameters.TryGetValue(ParameterManager.SpecialParamName.UserName, out var userName);
            param.parameters.TryGetValue(ParameterManager.SpecialParamName.UserPassword, out var userPassword);

            var sessionResult = SessionManager.Instance.CreateSession(ipAddress, (string)userName, (string)userPassword, param);
            var session = sessionResult.objs.FirstOrDefault()?.BaseObject;
            if (session == null)
            {
                result = sessionResult;
            }
            else
            {
                param.parameters.Add(ReservedParameterName.Session, session);
                result = psScript.Invoke(param);
            }


            return result;
        }

    }
}
