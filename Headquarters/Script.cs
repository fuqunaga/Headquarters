using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Headquarters
{
    class Script
    {
        public static class ReservedParameterNames
        {
            public const string IP = "ip";
            public const string Session = "session";
        }


        private List<string> paramNames;
        private readonly string filepath;
        private PowerShellScript psScript;

        public string Name => Path.GetFileNameWithoutExtension(filepath);

        public IEnumerable<string> ParamNamesForUI => paramNames.Where(n =>
        {
            var nl = n.ToLower();
            return nl != ReservedParameterNames.IP && nl != ReservedParameterNames.Session;
        });


        public Script(string filepath)
        {
            this.filepath = filepath;
            Load();
        }

        public void Load()
        {
            if (File.Exists(filepath))
            {
                var script = File.ReadAllText(filepath);

                psScript = new PowerShellScript(Name, script);

                paramNames = SearchParameters(script);
            }
        }

        List<string> SearchParameters(string script)
        {
            var match = Regex.Match(script, @"(?<=param\().*?(?=\))");
            return match.Value
                .Replace("$", "")
                .Replace(" ", "")
                .Split(',')
                .ToList();
        }

        public PowerShellScript.Result Run(string ipAddress, PowerShellScript.InvokeParameter param)
        {
            var needIP = HasParameter(ReservedParameterNames.IP);
            if (needIP)
            {
                param.parameters.Add(ReservedParameterNames.IP, ipAddress);
            }


            var needSession = HasParameter(ReservedParameterNames.Session);
            if (needSession)
            {

                param.parameters.TryGetValue(ParameterManager.SpecialParamName.UserName, out var userName);
                param.parameters.TryGetValue(ParameterManager.SpecialParamName.UserPassword, out var userPassword);

                var sessionResult = SessionManager.Instance.CreateSession(ipAddress, (string)userName, (string)userPassword, param);
                var session = sessionResult.objs.FirstOrDefault()?.BaseObject;
                if (session == null)
                {
                    return sessionResult;
                }
                else
                {
                    param.parameters.Add(ReservedParameterNames.Session, session);
                }
            }

            return psScript.Invoke(param);
        }

        private bool HasParameter(string parameterName) => paramNames.Any(n => n.ToLower() == parameterName.ToLower());
    }
}
